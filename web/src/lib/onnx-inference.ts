import type * as Ort from "onnxruntime-web";

export const MAX_LOCAL_ONNX_MODEL_BYTES = 256 * 1024 * 1024;
export const MULTI_ACTION_ONNX_OBSERVATION_DIM = 546;
export const MULTI_ACTION_ONNX_LEGAL_MASK_DIM = 276;
export const MULTI_ACTION_ONNX_OBSERVATION_INPUT = "observations";
export const MULTI_ACTION_ONNX_LEGAL_MASK_INPUT = "legal_action_mask";
export const MULTI_ACTION_ONNX_LOGITS_OUTPUT = "action_logits";

export type OnnxExecutionProvider = "webgpu" | "wasm";
export type OnnxTensorType = "bool" | "float32";

export interface LocalBinaryFile {
  readonly name: string;
  readonly size: number;
  readonly type: string;
  arrayBuffer(): Promise<ArrayBuffer>;
}

export interface OnnxTensorOutput {
  readonly data: unknown;
  readonly dims: readonly number[];
}

export interface OnnxSession {
  readonly inputNames: readonly string[];
  readonly outputNames: readonly string[];
  run(feeds: Readonly<Record<string, unknown>>): Promise<Readonly<Record<string, OnnxTensorOutput>>>;
  release(): Promise<void>;
}

export interface OnnxRuntime {
  createSession(
    model: Uint8Array,
    options: Readonly<{
      executionProviders: readonly OnnxExecutionProvider[];
      preferredOutputLocation: "cpu";
    }>
  ): Promise<OnnxSession>;
  createTensor(type: OnnxTensorType, data: Float32Array | Uint8Array, dims: readonly number[]): unknown;
}

export type OnnxRuntimeFactory = (provider: OnnxExecutionProvider) => Promise<OnnxRuntime>;

export interface BrowserMultiActionOnnxPolicy {
  readonly provider: OnnxExecutionProvider;
  readonly usedFallback: boolean;
  infer(observation: readonly number[], legalActionMask: readonly boolean[]): Promise<Float32Array>;
  release(): Promise<void>;
}

const ACCEPTED_MIME_TYPES = new Set(["application/octet-stream", "application/onnx"]);
const EXPECTED_INPUT_NAMES = [
  MULTI_ACTION_ONNX_OBSERVATION_INPUT,
  MULTI_ACTION_ONNX_LEGAL_MASK_INPUT
] as const;
const EXPECTED_OUTPUT_NAMES = [MULTI_ACTION_ONNX_LOGITS_OUTPUT] as const;

export function validateLocalOnnxFile(file: unknown): readonly string[] {
  if (!isLocalBinaryFile(file)) return ["choose a local ONNX file"];
  const errors: string[] = [];
  if (!isSafeOnnxName(file.name)) errors.push("file name must be a local .onnx name");
  if (!Number.isInteger(file.size) || file.size <= 0) errors.push("file must not be empty");
  if (file.size > MAX_LOCAL_ONNX_MODEL_BYTES) {
    errors.push(`file exceeds the ${MAX_LOCAL_ONNX_MODEL_BYTES / 1024 / 1024} MiB limit`);
  }
  if (file.type && !ACCEPTED_MIME_TYPES.has(file.type)) {
    errors.push("file type must be ONNX or binary data");
  }
  return Object.freeze(errors);
}

export async function loadLocalMultiActionOnnxPolicy(
  file: LocalBinaryFile,
  options: {
    readonly runtimeFactory?: OnnxRuntimeFactory;
    readonly webGpuAvailable?: boolean;
  } = {}
): Promise<BrowserMultiActionOnnxPolicy> {
  const fileErrors = validateLocalOnnxFile(file);
  if (fileErrors.length > 0) throw new Error(fileErrors.join("; "));
  const model = await readModelBytes(file);
  const runtimeFactory = options.runtimeFactory ?? defaultRuntimeFactory;
  const webGpuAvailable = options.webGpuAvailable ?? browserSupportsWebGpu();
  if (webGpuAvailable) {
    try {
      return await createPolicy(model, "webgpu", false, runtimeFactory);
    } catch {}
  }
  try {
    return await createPolicy(model, "wasm", webGpuAvailable, runtimeFactory);
  } catch {
    throw new Error("could not initialize the local ONNX model");
  }
}

async function createPolicy(
  model: Uint8Array,
  provider: OnnxExecutionProvider,
  usedFallback: boolean,
  runtimeFactory: OnnxRuntimeFactory
): Promise<BrowserMultiActionOnnxPolicy> {
  const runtime = await runtimeFactory(provider);
  const session = await runtime.createSession(model, {
    executionProviders: [provider],
    preferredOutputLocation: "cpu"
  });
  try {
    validateSessionContract(session);
  } catch (error) {
    await session.release();
    throw error;
  }
  return new LocalMultiActionOnnxPolicy(runtime, session, provider, usedFallback);
}

class LocalMultiActionOnnxPolicy implements BrowserMultiActionOnnxPolicy {
  #released = false;
  readonly #runtime: OnnxRuntime;
  readonly #session: OnnxSession;
  readonly provider: OnnxExecutionProvider;
  readonly usedFallback: boolean;

  constructor(
    runtime: OnnxRuntime,
    session: OnnxSession,
    provider: OnnxExecutionProvider,
    usedFallback: boolean
  ) {
    this.#runtime = runtime;
    this.#session = session;
    this.provider = provider;
    this.usedFallback = usedFallback;
  }

  async infer(observation: readonly number[], legalActionMask: readonly boolean[]): Promise<Float32Array> {
    if (this.#released) throw new Error("ONNX policy has been released");
    validateInferenceInput(observation, legalActionMask);
    const outputs = await this.#session.run({
      [MULTI_ACTION_ONNX_OBSERVATION_INPUT]: this.#runtime.createTensor(
        "float32",
        Float32Array.from(observation),
        [1, MULTI_ACTION_ONNX_OBSERVATION_DIM]
      ),
      [MULTI_ACTION_ONNX_LEGAL_MASK_INPUT]: this.#runtime.createTensor(
        "bool",
        Uint8Array.from(legalActionMask, (value) => (value ? 1 : 0)),
        [1, MULTI_ACTION_ONNX_LEGAL_MASK_DIM]
      )
    });
    return readLogits(outputs[MULTI_ACTION_ONNX_LOGITS_OUTPUT]);
  }

  async release(): Promise<void> {
    if (this.#released) return;
    this.#released = true;
    await this.#session.release();
  }
}

function validateSessionContract(session: OnnxSession): void {
  if (!sameNames(session.inputNames, EXPECTED_INPUT_NAMES)) {
    throw new Error("ONNX model inputs do not match the multi-action contract");
  }
  if (!sameNames(session.outputNames, EXPECTED_OUTPUT_NAMES)) {
    throw new Error("ONNX model outputs do not match the multi-action contract");
  }
}

function validateInferenceInput(observation: readonly number[], legalActionMask: readonly boolean[]): void {
  if (observation.length !== MULTI_ACTION_ONNX_OBSERVATION_DIM) {
    throw new Error(`observation must contain ${MULTI_ACTION_ONNX_OBSERVATION_DIM} values`);
  }
  if (!observation.every(Number.isFinite)) throw new Error("observation values must be finite");
  if (legalActionMask.length !== MULTI_ACTION_ONNX_LEGAL_MASK_DIM) {
    throw new Error(`legal action mask must contain ${MULTI_ACTION_ONNX_LEGAL_MASK_DIM} values`);
  }
  if (!legalActionMask.some(Boolean)) throw new Error("legal action mask must allow at least one action");
}

function readLogits(output: OnnxTensorOutput | undefined): Float32Array {
  if (!output || !sameNumbers(output.dims, [1, MULTI_ACTION_ONNX_LEGAL_MASK_DIM])) {
    throw new Error("ONNX model returned invalid action logits");
  }
  if (!(output.data instanceof Float32Array) || output.data.length !== MULTI_ACTION_ONNX_LEGAL_MASK_DIM) {
    throw new Error("ONNX model returned invalid action logits");
  }
  return new Float32Array(output.data);
}

async function readModelBytes(file: LocalBinaryFile): Promise<Uint8Array> {
  let source: unknown;
  try {
    source = await file.arrayBuffer();
  } catch {
    throw new Error("local ONNX file could not be read");
  }
  if (!(source instanceof ArrayBuffer) || source.byteLength !== file.size) {
    throw new Error("local ONNX file could not be read");
  }
  return new Uint8Array(source);
}

async function defaultRuntimeFactory(provider: OnnxExecutionProvider): Promise<OnnxRuntime> {
  const ort = provider === "webgpu"
    ? await import("onnxruntime-web/webgpu")
    : await import("onnxruntime-web");
  return adaptOrtRuntime(ort);
}

function adaptOrtRuntime(ort: typeof Ort): OnnxRuntime {
  return {
    createSession: async (model, options) => {
      const session = await ort.InferenceSession.create(model, options);
      return {
        inputNames: session.inputNames,
        outputNames: session.outputNames,
        run: async (feeds) => session.run(feeds as Ort.InferenceSession.FeedsType) as unknown as Readonly<Record<string, OnnxTensorOutput>>,
        release: () => session.release()
      };
    },
    createTensor: (type, data, dims) => type === "float32"
      ? new ort.Tensor("float32", data as Float32Array, dims)
      : new ort.Tensor("bool", data as Uint8Array, dims)
  };
}

function browserSupportsWebGpu(): boolean {
  return typeof navigator !== "undefined" && "gpu" in navigator;
}

function isLocalBinaryFile(value: unknown): value is LocalBinaryFile {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const file = value as Partial<LocalBinaryFile>;
  return (
    typeof file.name === "string" &&
    typeof file.size === "number" &&
    typeof file.type === "string" &&
    typeof file.arrayBuffer === "function"
  );
}

function isSafeOnnxName(name: string): boolean {
  return Boolean(name) && !/[\\/\0]/.test(name) && name.toLowerCase().endsWith(".onnx");
}

function sameNames(actual: readonly string[], expected: readonly string[]): boolean {
  return actual.length === expected.length && expected.every((name) => actual.includes(name));
}

function sameNumbers(actual: readonly number[], expected: readonly number[]): boolean {
  return actual.length === expected.length && expected.every((value, index) => actual[index] === value);
}

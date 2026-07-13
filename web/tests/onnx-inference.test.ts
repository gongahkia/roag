import assert from "node:assert/strict";
import test from "node:test";

import {
  MAX_LOCAL_ONNX_MODEL_BYTES,
  MULTI_ACTION_ONNX_LEGAL_MASK_DIM,
  MULTI_ACTION_ONNX_OBSERVATION_DIM,
  loadLocalMultiActionOnnxPolicy,
  validateLocalOnnxFile,
  type LocalBinaryFile,
  type OnnxExecutionProvider,
  type OnnxRuntime,
  type OnnxRuntimeFactory
} from "../src/lib/onnx-inference";

test("runs a local ONNX model with WebGPU when it is available", async () => {
  const providers: OnnxExecutionProvider[] = [];
  const tensors: unknown[] = [];
  const runtime = fakeRuntime((feeds) => {
    tensors.push(...Object.values(feeds));
    return logitsOutput();
  });
  const policy = await loadLocalMultiActionOnnxPolicy(localModel(), {
    runtimeFactory: recordingFactory(providers, runtime),
    webGpuAvailable: true
  });

  const logits = await policy.infer(
    Array.from({ length: MULTI_ACTION_ONNX_OBSERVATION_DIM }, (_, index) => index / 10),
    legalMask(3)
  );

  assert.deepEqual(providers, ["webgpu"]);
  assert.equal(policy.provider, "webgpu");
  assert.equal(policy.usedFallback, false);
  assert.equal(logits.length, MULTI_ACTION_ONNX_LEGAL_MASK_DIM);
  assert.equal(tensors.length, 2);
  await policy.release();
  await policy.release();
  assert.equal(runtime.releases, 1);
});

test("falls back to WebAssembly when WebGPU initialization fails", async () => {
  const providers: OnnxExecutionProvider[] = [];
  const wasmRuntime = fakeRuntime(() => logitsOutput());
  const policy = await loadLocalMultiActionOnnxPolicy(localModel(), {
    runtimeFactory: async (provider) => {
      providers.push(provider);
      if (provider === "webgpu") throw new Error("unavailable");
      return wasmRuntime;
    },
    webGpuAvailable: true
  });

  assert.deepEqual(providers, ["webgpu", "wasm"]);
  assert.equal(policy.provider, "wasm");
  assert.equal(policy.usedFallback, true);
  await policy.release();
});

test("uses WebAssembly directly when WebGPU is unavailable", async () => {
  const providers: OnnxExecutionProvider[] = [];
  const policy = await loadLocalMultiActionOnnxPolicy(localModel(), {
    runtimeFactory: recordingFactory(providers, fakeRuntime(() => logitsOutput())),
    webGpuAvailable: false
  });

  assert.deepEqual(providers, ["wasm"]);
  assert.equal(policy.usedFallback, false);
  await policy.release();
});

test("rejects non-local models and invalid inference contracts before execution", async () => {
  assert.deepEqual(validateLocalOnnxFile(localModel("https://example.invalid/model.onnx")), [
    "file name must be a local .onnx name"
  ]);
  assert.deepEqual(validateLocalOnnxFile(localModel("model.bin", "text/html")), [
    "file name must be a local .onnx name",
    "file type must be ONNX or binary data"
  ]);
  assert.deepEqual(validateLocalOnnxFile(localModel("model.onnx", "", MAX_LOCAL_ONNX_MODEL_BYTES + 1)), [
    `file exceeds the ${MAX_LOCAL_ONNX_MODEL_BYTES / 1024 / 1024} MiB limit`
  ]);
  const runtime = fakeRuntime(() => logitsOutput());
  const policy = await loadLocalMultiActionOnnxPolicy(localModel(), {
    runtimeFactory: async () => runtime,
    webGpuAvailable: false
  });

  await assert.rejects(policy.infer([], legalMask(0)), /observation must contain/);
  await assert.rejects(
    policy.infer(Array(MULTI_ACTION_ONNX_OBSERVATION_DIM).fill(0), Array(MULTI_ACTION_ONNX_LEGAL_MASK_DIM).fill(false)),
    /at least one action/
  );
  assert.equal(runtime.runs, 0);
  await policy.release();
});

test("rejects a model with an incompatible graph contract", async () => {
  const runtime = fakeRuntime(() => logitsOutput(), ["input"], ["output"]);
  await assert.rejects(
    loadLocalMultiActionOnnxPolicy(localModel(), {
      runtimeFactory: async () => runtime,
      webGpuAvailable: false
    }),
    /could not initialize/
  );
  assert.equal(runtime.releases, 1);
});

function localModel(name = "model.onnx", type = "application/onnx", size?: number): LocalBinaryFile {
  const bytes = new Uint8Array([8, 6, 7, 5, 3, 0, 9]);
  return {
    name,
    type,
    size: size ?? bytes.byteLength,
    arrayBuffer: async () => bytes.buffer.slice(0)
  };
}

function legalMask(index: number): boolean[] {
  return Array.from({ length: MULTI_ACTION_ONNX_LEGAL_MASK_DIM }, (_, value) => value === index);
}

function logitsOutput() {
  return {
    action_logits: {
      data: Float32Array.from({ length: MULTI_ACTION_ONNX_LEGAL_MASK_DIM }, (_, index) => index),
      dims: [1, MULTI_ACTION_ONNX_LEGAL_MASK_DIM]
    }
  };
}

function recordingFactory(providers: OnnxExecutionProvider[], runtime: OnnxRuntime): OnnxRuntimeFactory {
  return async (provider) => {
    providers.push(provider);
    return runtime;
  };
}

function fakeRuntime(
  output: (feeds: Readonly<Record<string, unknown>>) => ReturnType<typeof logitsOutput>,
  inputNames = ["observations", "legal_action_mask"],
  outputNames = ["action_logits"]
): OnnxRuntime & { releases: number; runs: number } {
  let releases = 0;
  let runs = 0;
  return {
    get releases() { return releases; },
    get runs() { return runs; },
    createTensor: (type, data, dims) => ({ type, data, dims }),
    createSession: async () => ({
      inputNames,
      outputNames,
      run: async (feeds) => {
        runs += 1;
        return output(feeds);
      },
      release: async () => { releases += 1; }
    })
  };
}

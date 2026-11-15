import 'dart:js_interop';

@JS('settlementPlatform.load')
external JSString _load();
@JS('settlementPlatform.save')
external void _save(JSString text);
@JS('settlementPlatform.exportFile')
external void _export(JSString text);
@JS('settlementPlatform.importFile')
external JSPromise<JSString> _import();
@JS('settlementPlatform.sound')
external void _sound(JSString kind);
@JS('settlementPlatform.mute')
external void _mute(JSBoolean value);
@JS('settlementPlatform.hideLoading')
external void hideLoading();
@JS('settlementPlatform.watchActivity')
external void _watchActivity(JSFunction callback);
@JS('settlementPlatform.unwatchActivity')
external void unwatchActivity();
void watchActivity(void Function(bool) callback) =>
    _watchActivity(((JSBoolean active) => callback(active.toDart)).toJS);

String loadLocal() => _load().toDart;
void saveLocal(String text) => _save(text.toJS);
void exportFile(String text) => _export(text.toJS);
Future<String> importFile() async => (await _import().toDart).toDart;
void sound(String kind) => _sound(kind.toJS);
void mute(bool value) => _mute(value.toJS);

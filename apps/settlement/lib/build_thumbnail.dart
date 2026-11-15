import 'package:flame/components.dart';
import 'package:material_ui/material_ui.dart';

import 'build_art.dart';
import 'model.dart';

class BuildThumbnail extends StatelessWidget {
  final Tool tool;
  final Map<String, Sprite> art;
  final double size;
  const BuildThumbnail(this.tool, this.art, {this.size = 36, super.key});
  @override
  Widget build(BuildContext context) => SizedBox.square(
    dimension: size,
    child: CustomPaint(painter: _Thumbnail(tool, art)),
  );
}

class _Thumbnail extends CustomPainter {
  final Tool tool;
  final Map<String, Sprite> art;
  final bool ready;
  _Thumbnail(this.tool, this.art)
    : ready = art.containsKey(BuildArt.frameFor(tool));
  @override
  void paint(Canvas canvas, Size size) {
    canvas.save();
    canvas.scale(size.width / 12, size.height / 12);
    canvas.clipRect(const Rect.fromLTWH(0, 0, 12, 12), doAntiAlias: false);
    BuildArt(art).thumbnail(canvas, tool);
    canvas.restore();
  }

  @override
  bool shouldRepaint(_Thumbnail old) =>
      old.tool != tool || old.ready != ready || !identical(old.art, art);
}

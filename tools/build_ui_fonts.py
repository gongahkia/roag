#!/usr/bin/env python3
"""Build text glyphs from the bundled, credited fonts; game sprites still use the universal atlas.

Requires a C compiler, pkg-config and FreeType development headers only when
regenerating. The Linux game consumes the checked-in PNG/JSON, without FreeType.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import tempfile
from asset_catalog import encode_rgba_png

ROOT = Path(__file__).resolve().parents[1]
HELPER = r'''
#include <ft2build.h>
#include FT_FREETYPE_H
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
int main(int argc,char**argv) {
 FT_Library library; FT_Face face;
 if(argc!=4 || FT_Init_FreeType(&library) || FT_New_Face(library,argv[1],0,&face) || FT_Set_Pixel_Sizes(face,0,atoi(argv[2]))) return 1;
 int flags=FT_LOAD_RENDER | (atoi(argv[3]) ? FT_LOAD_TARGET_MONO | FT_LOAD_MONOCHROME : FT_LOAD_TARGET_NORMAL);
 for(int code=32;code<=126;code++) {
  if(FT_Load_Char(face,code,flags))return 2;
  FT_GlyphSlot g=face->glyph;FT_Bitmap *b=&g->bitmap;
  int32_t metrics[]={code,(g->advance.x+32)/64,b->width,b->rows,g->bitmap_left,g->bitmap_top};
  fwrite(metrics,sizeof(int32_t),6,stdout);
  for(unsigned int y=0;y<b->rows;y++)for(unsigned int x=0;x<b->width;x++) {
   unsigned char*row=b->buffer+y*b->pitch;
   unsigned char value=b->pixel_mode==FT_PIXEL_MODE_MONO ? (row[x/8]&(128>>(x%8)) ? 255:0):row[x];
   fwrite(&value,1,1,stdout);
  }
 }
 FT_Done_Face(face);FT_Done_FreeType(library);return 0;
}
'''

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--check',action='store_true');args=parser.parse_args()
    fonts=ROOT/'assets/fonts';width,height=384,864;pixels=bytearray(width*height*4);faces=[]
    with tempfile.TemporaryDirectory(prefix='roag-font-build-') as tmp:
        source=Path(tmp)/'glyphs.c';exe=Path(tmp)/'glyphs';source.write_text(HELPER)
        flags=subprocess.check_output(['pkg-config','--cflags','--libs','freetype2'],text=True).split()
        subprocess.run(['cc',str(source),'-o',str(exe),*flags],check=True)
        for style,filename in [('Pixel','BigBlue_TerminalPlus.ttf'),('Sans','SourceCodePro-Regular.otf')]:
            for size,points in [('Small',10),('Normal',12),('Large',14)]:
                data=subprocess.check_output([str(exe),str(fonts/filename),str(points),'1' if style=='Pixel'else'0']);offset=0;glyphs=[]
                band=len(faces)*144
                for index in range(95):
                    code,advance,w,h,left,top=struct.unpack_from('<6i',data,offset);offset+=24
                    assert w<=20 and h<=20 and advance>0
                    x=index%16*24+2;y=band+index//16*24+2
                    for yy in range(h):
                        for xx in range(w):
                            alpha=data[offset+yy*w+xx];dest=((y+yy)*width+x+xx)*4
                            pixels[dest:dest+4]=bytes([255,255,255,alpha])
                    offset+=w*h;glyphs.append(dict(code=code,rect=[x,y,w,h],left=left,top=top,advance=advance))
                faces.append(dict(style=style,size=size,source=filename,sha256=hashlib.sha256((fonts/filename).read_bytes()).hexdigest(),
                    ascent=max(g['top']for g in glyphs),advance=max(g['advance']for g in glyphs),glyphs=glyphs))
    metadata=(json.dumps(dict(version=1,texture='ui-fonts.png',faces=faces),separators=(',',':'))+'\n').encode()
    outputs={'ui-fonts.png':encode_rgba_png(width,height,bytes(pixels)),'ui-fonts.json':metadata}
    for name,data in outputs.items():
        path=fonts/name
        if args.check:
            if not path.exists() or path.read_bytes()!=data: raise SystemExit(f'Stale font output: {path}')
        else:path.write_bytes(data)
    print('Verified'if args.check else'Built','six text faces (570 glyphs); game atlas bindings unchanged.')

if __name__=='__main__':main()

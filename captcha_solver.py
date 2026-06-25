import sys
import ddddocr

ocr = ddddocr.DdddOcr(show_ad=False)

with open(sys.argv[1], "rb") as f:
    img_bytes = f.read()

result = ocr.classification(img_bytes)
print(result)
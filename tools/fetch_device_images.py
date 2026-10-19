"""Download product photos for the model selector.

They are NOT stored in git (they belong to their manufacturers). Run once after cloning:

  pip install pillow
  python tools/fetch_device_images.py

Reads imageUrl from app/KsFit.Core/devices.json and writes
app/KsFit.App/Assets/devices/<id>.png (max 320 px). The app shows a placeholder for missing images.
"""
import io, json, pathlib, sys, urllib.request
from PIL import Image

root = pathlib.Path(__file__).resolve().parent.parent
out = root / "app/KsFit.App/Assets/devices"
out.mkdir(parents=True, exist_ok=True)
for m in json.load(open(root / "app/KsFit.Core/devices.json", encoding="utf-8")):
    if not m.get("imageUrl"):
        continue
    dest = out / f"{m['id']}.png"
    if dest.exists():
        continue
    try:
        req = urllib.request.Request(m["imageUrl"] + "?width=480", headers={"User-Agent": "Mozilla/5.0"})
        im = Image.open(io.BytesIO(urllib.request.urlopen(req, timeout=30).read())).convert("RGB")
        im.thumbnail((320, 320))
        im.save(dest)
        print("ok  ", m["id"])
    except Exception as e:
        print("FAIL", m["id"], e, file=sys.stderr)

"""Original app icon (no third-party artwork): dark rounded tile, teal walker on a belt."""
from PIL import Image, ImageDraw
import sys

S = 1024
im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.rounded_rectangle((40, 40, S - 40, S - 40), radius=230, fill=(24, 27, 34, 255))
d.rounded_rectangle((40, 40, S - 40, S - 40), radius=230, outline=(79, 209, 197, 255), width=22)

teal = (79, 209, 197, 255)
w = 60


def line(pts):
    d.line(pts, fill=teal, width=w, joint="curve")
    for x, y in (pts[0], pts[-1]):
        d.ellipse((x - w // 2, y - w // 2, x + w // 2, y + w // 2), fill=teal)


# belt
d.rounded_rectangle((190, 760, 834, 830), radius=35, fill=(107, 115, 133, 255))
# walker (mid-stride)
d.ellipse((520, 190, 640, 310), fill=teal)                       # head
line([(560, 340), (520, 520), (500, 640)])                        # torso
line([(500, 640), (610, 700), (650, 760)])                        # front leg
line([(500, 640), (400, 710), (330, 750)])                        # back leg
line([(545, 380), (640, 470), (700, 450)])                        # front arm
line([(545, 380), (450, 450), (400, 520)])                        # back arm

out = sys.argv[1]
sizes = [16, 24, 32, 48, 64, 128, 256]
im.resize((256, 256), Image.LANCZOS).save(out, format="ICO", sizes=[(s, s) for s in sizes])
im.resize((256, 256), Image.LANCZOS).save(out.replace(".ico", ".png"))
print("ok")

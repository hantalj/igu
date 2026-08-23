"""Regenerates the main menu art (background, button, title) from source
files. Not run automatically -- a reference/reproducibility script, same
role as igrunner's gen_placeholders.py.

Requires: pip install --user Pillow numpy scipy

Usage: python3 generate_menu_art.py <path/to/iceland_background.jpg>

The background source photo isn't checked into this repo (it's the user's
own photo, not game art); point this at wherever you kept a copy.
"""
import sys
import numpy as np
from scipy.ndimage import uniform_filter
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance, ImageFont

HERE = __import__("os").path.dirname(__import__("os").path.abspath(__file__))


def make_painted_background(source_path, out_path):
	"""Oil-paint stylization: for each pixel, average the color of same-
	brightness-level neighbors within a window (the classic oil-paint
	algorithm), which gives real brush-stroke blocking -- a naive
	blur+posterize pass reads as 'flattened photo', not 'painting', and
	produces color-fringing artifacts at high-contrast edges.
	"""
	src = Image.open(source_path).convert("RGB")

	scale = 0.5
	small = src.resize((int(src.width * scale), int(src.height * scale)), Image.LANCZOS)
	arr = np.asarray(small).astype(np.float32)

	intensity = arr.mean(axis=2)
	levels = 24
	q = np.clip((intensity / 256 * levels).astype(np.int32), 0, levels - 1)

	radius = 5
	size = radius * 2 + 1
	area = size * size

	best_count = np.zeros(q.shape, dtype=np.float32)
	best_sum = np.zeros(arr.shape, dtype=np.float32)

	for level in range(levels):
		mask = (q == level).astype(np.float32)
		count = uniform_filter(mask, size=size) * area
		masked = arr * mask[..., None]
		sums = np.stack([uniform_filter(masked[..., c], size=size) * area for c in range(3)], axis=-1)
		better = count > best_count
		best_count = np.where(better, count, best_count)
		best_sum = np.where(better[..., None], sums, best_sum)

	out = best_sum / np.maximum(best_count[..., None], 1)
	out = np.clip(out, 0, 255).astype(np.uint8)
	painted = Image.fromarray(out).resize(src.size, Image.LANCZOS)

	painted = ImageEnhance.Color(painted).enhance(1.25)
	painted = ImageEnhance.Contrast(painted).enhance(1.12)
	overlay = Image.new("RGB", painted.size, (18, 28, 42))
	painted = Image.blend(painted, overlay, 0.15)
	painted = painted.filter(ImageFilter.UnsharpMask(radius=4, percent=70, threshold=2))

	w, h = painted.size
	vign = Image.new("L", (w, h), 0)
	vd = ImageDraw.Draw(vign)
	for i in range(255):
		inset = int(i * (min(w, h) / 2) / 255)
		vd.rectangle([inset, inset, w - inset, h - inset], fill=i)
	vign = vign.filter(ImageFilter.GaussianBlur(90))
	dark = Image.new("RGB", (w, h), (0, 0, 0))
	painted = Image.composite(painted, dark, vign.point(lambda p: int(p * 0.8 + 50)))

	painted.save(out_path)


def make_button(out_path, size=(700, 110), radius=20,
				 fill_top=(46, 40, 34), fill_bottom=(24, 20, 17),
				 border=(196, 156, 82), border_hi=(238, 205, 128), border_lo=(107, 78, 34),
				 gem=(72, 168, 165), gem_hi=(178, 236, 230)):
	"""Original ornate panel design (dark bevelled parchment/gold frame,
	gem accents) -- styled in the spirit of dark-fantasy game menus, not
	copied from any specific game's actual artwork/logo/font.
	"""
	def rounded_mask(sz, r):
		m = Image.new("L", sz, 0)
		ImageDraw.Draw(m).rounded_rectangle([0, 0, sz[0] - 1, sz[1] - 1], radius=r, fill=255)
		return m

	def vertical_gradient(sz, top, bottom):
		w, h = sz
		grad = np.zeros((h, w, 3), dtype=np.uint8)
		for y in range(h):
			t = y / max(h - 1, 1)
			grad[y, :, :] = tuple(int(top[c] * (1 - t) + bottom[c] * t) for c in range(3))
		return Image.fromarray(grad)

	w, h = size
	canvas = Image.new("RGBA", size, (0, 0, 0, 0))

	outer = rounded_mask(size, radius)
	canvas.paste(Image.new("RGBA", size, border_hi + (255,)), (0, 0), outer)
	canvas.paste(Image.new("RGBA", (w - 4, h - 4), border + (255,)), (2, 3), rounded_mask((w - 4, h - 4), radius - 2))
	canvas.paste(Image.new("RGBA", (w - 4, h - 6), border + (255,)), (2, 2), rounded_mask((w - 4, h - 6), radius - 2))
	canvas.paste(Image.new("RGBA", (w - 6, h - 6), border + (255,)), (3, 3), rounded_mask((w - 6, h - 6), radius - 3))

	pad = 9
	inner_size = (w - pad * 2, h - pad * 2)
	grad = vertical_gradient(inner_size, fill_top, fill_bottom)
	canvas.paste(grad, (pad, pad), rounded_mask(inner_size, max(radius - pad, 4)))

	hairline = Image.new("RGBA", size, (0, 0, 0, 0))
	ImageDraw.Draw(hairline).rounded_rectangle(
		[pad, pad, w - pad - 1, h - pad - 1], radius=max(radius - pad, 4), outline=(10, 8, 6, 180), width=2)
	canvas = Image.alpha_composite(canvas, hairline)

	def draw_gem(cx, cy, r):
		gd = ImageDraw.Draw(canvas)
		gd.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)],
				   fill=gem + (255,), outline=border_lo + (255,))
		r2 = int(r * 0.5)
		gd.polygon([(cx, cy - r2), (cx + r2, cy - int(r2 * 0.15)), (cx, cy - int(r2 * 0.4)), (cx - r2, cy - int(r2 * 0.15))],
				   fill=gem_hi + (255,))

	gem_r = int(h * 0.16)
	draw_gem(int(w * 0.075), h // 2, gem_r)
	draw_gem(int(w * 0.925), h // 2, gem_r)

	shadow = Image.new("RGBA", (w + 20, h + 20), (0, 0, 0, 0))
	shadow.paste(Image.new("RGBA", size, (0, 0, 0, 160)), (10, 12), outer)
	shadow = shadow.filter(ImageFilter.GaussianBlur(6))
	final = Image.new("RGBA", (w + 20, h + 20), (0, 0, 0, 0))
	final = Image.alpha_composite(final, shadow)
	final.alpha_composite(canvas, (10, 8))
	final.save(out_path)


def make_title(out_path, text="Igu", size=(900, 340), font_size=220,
			   font_path="/System/Library/Fonts/Supplemental/Georgia Bold.ttf"):
	"""Renders to a bitmap once -- the font file itself is never copied into
	the repo (Georgia is a commercial font; shipping the .ttf would be
	redistributing it, shipping a pre-rendered PNG of it is not).
	"""
	w, h = size
	canvas = Image.new("RGBA", size, (0, 0, 0, 0))
	font = ImageFont.truetype(font_path, font_size)

	mask = Image.new("L", size, 0)
	md = ImageDraw.Draw(mask)
	bbox = md.textbbox((0, 0), text, font=font)
	tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
	pos = ((w - tw) // 2 - bbox[0], (h - th) // 2 - bbox[1])
	md.text(pos, text, font=font, fill=255)

	glow_solid = Image.new("RGBA", size, (255, 170, 60, 255))
	for blur, in [(14,), (28,)]:
		glow = Image.new("RGBA", size, (0, 0, 0, 0))
		glow.paste(glow_solid, (0, 0), mask)
		canvas = Image.alpha_composite(canvas, glow.filter(ImageFilter.GaussianBlur(blur)))

	outline_mask = mask.filter(ImageFilter.MaxFilter(7))
	outline_img = Image.new("RGBA", size, (0, 0, 0, 0))
	outline_img.paste(Image.new("RGBA", size, (35, 18, 8, 255)), (0, 0), outline_mask)
	canvas = Image.alpha_composite(canvas, outline_img)

	grad = np.zeros((h, w, 3), dtype=np.uint8)
	top, bottom = (255, 231, 165), (196, 138, 51)
	for y in range(h):
		t = y / (h - 1)
		grad[y, :, :] = tuple(int(top[c] * (1 - t) + bottom[c] * t) for c in range(3))
	fill_img = Image.new("RGBA", size, (0, 0, 0, 0))
	fill_img.paste(Image.fromarray(grad).convert("RGBA"), (0, 0), mask)
	canvas = Image.alpha_composite(canvas, fill_img)

	canvas.save(out_path)


if __name__ == "__main__":
	if len(sys.argv) != 2:
		print(__doc__)
		sys.exit(1)
	make_painted_background(sys.argv[1], f"{HERE}/menu_background.png")
	make_button(f"{HERE}/button_normal.png")
	make_title(f"{HERE}/title_igu.png")
	print("Generated menu_background.png, button_normal.png, title_igu.png")

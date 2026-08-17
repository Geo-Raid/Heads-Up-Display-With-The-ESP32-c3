from PIL import Image
import os
import sys

WIDTH = 320
HEIGHT = 172

EXPECTED_SIZE = WIDTH * HEIGHT


def rgb332_to_rgb(pixel):
    # RGB332:
    # RRR GGG BB

    r = (pixel >> 5) & 0b111
    g = (pixel >> 2) & 0b111
    b = pixel & 0b11

    # Expand to 8-bit
    r = (r * 255) // 7
    g = (g * 255) // 7
    b = (b * 255) // 3

    return r, g, b


def convert_bin(filename):

    with open(filename, "rb") as f:
        data = f.read()

    print(f"Read {len(data)} bytes")

    if len(data) != EXPECTED_SIZE:
        print(f"ERROR: Expected {EXPECTED_SIZE} bytes")
        print(f"       Got      {len(data)} bytes")
        return

    image = Image.new("RGB", (WIDTH, HEIGHT))
    pixels = image.load()

    for y in range(HEIGHT):

        # Data starts at the bottom and goes upwards
        image_y = HEIGHT - 1 - y

        for x in range(WIDTH):

            index = y * WIDTH + x

            pixels[x, image_y] = rgb332_to_rgb(data[index])

    output = os.path.splitext(filename)[0] + ".png"

    image.save(output)

    print(f"Image saved to:")
    print(output)


if __name__ == "__main__":

    if len(sys.argv) < 2:
        print("Usage:")
        print("    python rgb332_viewer.py image.bin")
        sys.exit(1)

    convert_bin(sys.argv[1])
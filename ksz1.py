"""KS Fit Z1 remote over standard FTMS (Bluetooth fitness machine profile).

  python ksz1.py                 # interactive: s=start x=stop +/-=speed, number=set km/h, q=quit
  python ksz1.py --addr XX:XX:.. # skip scan
"""
import asyncio, argparse, struct, sys, msvcrt
from bleak import BleakClient, BleakScanner

FTMS = "00001826-0000-1000-8000-00805f9b34fb"
DATA, CTRL, STATUS = (f"{n:08x}-0000-1000-8000-00805f9b34fb" for n in (0x2acd, 0x2ad9, 0x2ada))
RANGE = f"{0x2ad4:08x}-0000-1000-8000-00805f9b34fb"
OK = {1: "ok", 2: "not supported", 3: "bad param", 4: "failed", 5: "no control"}


class Z1:
    def __init__(self, client):
        self.c, self.speed, self.lo, self.hi, self.step = client, 0.0, 1.0, 6.0, 0.1
        self.resp = asyncio.Queue()

    async def setup(self):
        lo, hi, st = struct.unpack("<HHH", await self.c.read_gatt_char(RANGE))
        self.lo, self.hi, self.step = lo / 100, hi / 100, st / 100
        await self.c.start_notify(DATA, self._data)
        await self.c.start_notify(STATUS, lambda h, d: print(f"\n[status] {d.hex()}"))
        await self.c.start_notify(CTRL, self._ctrl)  # indications
        await self.cmd(0x00)  # request control

    def _data(self, _, d):
        flags = struct.unpack_from("<H", d)[0]
        if not flags & 1:  # bit0 clear => instantaneous speed present
            self.speed = struct.unpack_from("<H", d, 2)[0] / 100
        print(f"\rspeed {self.speed:4.1f} km/h   ", end="", flush=True)

    def _ctrl(self, _, d):  # 0x80, request opcode, result
        if d[0] == 0x80:
            self.resp.put_nowait(d[2])

    async def cmd(self, op, payload=b""):
        await self.c.write_gatt_char(CTRL, bytes([op]) + payload, response=True)
        try:
            r = await asyncio.wait_for(self.resp.get(), 3)
            print(f"\n[cmd {op:#04x}] {OK.get(r, r)}")
        except asyncio.TimeoutError:
            print(f"\n[cmd {op:#04x}] no response")

    async def start(self): await self.cmd(0x07)
    async def stop(self): await self.cmd(0x08, b"\x01")

    async def set_speed(self, kmh):
        kmh = round(min(max(kmh, self.lo), self.hi), 2)
        await self.cmd(0x02, struct.pack("<H", int(kmh * 100)))


async def find():
    print("scanning for FTMS devices...")
    for d, adv in (await BleakScanner.discover(timeout=8, return_adv=True)).values():
        if FTMS in adv.service_uuids:
            print("found", d.address, d.name)
            return d.address
    sys.exit("no FTMS device found")


async def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--addr")
    a = ap.parse_args()
    async with BleakClient(a.addr or await find(), timeout=20) as c:
        z = Z1(c)
        await z.setup()
        print(f"connected. range {z.lo}-{z.hi} km/h. keys: s start, x stop, +/- speed, 1-6 set speed, q quit")
        buf = ""
        while True:
            await asyncio.sleep(0.05)
            if not msvcrt.kbhit():
                continue
            k = msvcrt.getwch()
            if k == "q": break
            elif k == "s": await z.start()
            elif k == "x": await z.stop()
            elif k in "+=": await z.set_speed(z.speed + z.step * 5)
            elif k in "-_": await z.set_speed(z.speed - z.step * 5)
            elif k.isdigit(): await z.set_speed(float(k))


asyncio.run(main())

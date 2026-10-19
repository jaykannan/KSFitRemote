import asyncio,struct
from bleak import BleakClient
A="82:20:00:0F:33:E4"
C=f"{0x2ad9:08x}-0000-1000-8000-00805f9b34fb"; D=f"{0x2acd:08x}-0000-1000-8000-00805f9b34fb"; S=f"{0x2ada:08x}-0000-1000-8000-00805f9b34fb"
sp=[0.0]; resp=[]
async def main():
    async with BleakClient(A,timeout=20) as c:
        def data(h,d): sp[0]=struct.unpack_from("<H",d,2)[0]/100
        await c.start_notify(C,lambda h,d:resp.append(d.hex()))
        await c.start_notify(D,data)
        await c.start_notify(S,lambda h,d:print("   status",d.hex()))
        async def cmd(name,b,wait=6):
            resp.clear()
            await c.write_gatt_char(C,b,response=True)
            await asyncio.sleep(wait)
            print(f"{name:22} resp={resp} speed_now={sp[0]}")
        await cmd("request control",b"\x00",1)
        await cmd("set 2.0 km/h",b"\x02"+struct.pack("<H",200),8)
        await cmd("set 1.5 km/h",b"\x02"+struct.pack("<H",150),8)
        await cmd("set 0.5 (below min)",b"\x02"+struct.pack("<H",50),3)
        await cmd("set 2.0 again",b"\x02"+struct.pack("<H",200),6)
        await cmd("pause (08 02)",b"\x08\x02",6)
        await cmd("resume start (07)",b"\x07",10)
        await cmd("stop (08 01)",b"\x08\x01",8)
        await cmd("start after stop",b"\x07",10)
        await cmd("final stop",b"\x08\x01",8)
asyncio.run(main())

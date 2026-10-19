import asyncio,sys
from bleak import BleakClient
A=sys.argv[1]
async def main():
    async with BleakClient(A, timeout=20) as c:
        for s in c.services:
            print("SERVICE",s.uuid)
            for ch in s.characteristics:
                v=''
                if 'read' in ch.properties:
                    try: v=(await c.read_gatt_char(ch)).hex()
                    except Exception as e: v='ERR '+str(e)[:30]
                print("  ",ch.uuid[:8],ch.properties,v)
        def cb(h,d): print("NOTIFY",h.uuid[:8],d.hex())
        for s in c.services:
            for ch in s.characteristics:
                if 'notify' in ch.properties:
                    try: await c.start_notify(ch,cb)
                    except Exception as e: print("notify fail",ch.uuid[:8],e)
        await asyncio.sleep(6)
asyncio.run(main())

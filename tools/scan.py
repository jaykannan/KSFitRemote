import asyncio
from bleak import BleakScanner
async def main():
    r = await BleakScanner.discover(timeout=12, return_adv=True)
    for a,(d,adv) in sorted(r.items(), key=lambda x:-x[1][1].rssi):
        print(a, adv.rssi, repr(d.name or adv.local_name), [u[4:8] if u.endswith('-0000-1000-8000-00805f9b34fb') else u for u in adv.service_uuids], {k:v.hex() for k,v in adv.manufacturer_data.items()})
asyncio.run(main())

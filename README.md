# HKX to Navmesh ( *.nva | *.nvmhktbnd )

## How to use
Requirements: [.NET Runtime 10 OR .NET Desktop Runtime 10 OR SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Drag and drop one or more HKX files onto the `ER_HKX2Navmesh.exe`

On first use or when not set it will ask you for a output folder. If you select your `mod` folder, it will then output the navmesh files in for example `mod\map\m11\m11_10_00_00\`

After that it will ask you for a map id, the accepted patterns will be shown. The next time you run the tool it will ask you if you want to use the same map id as last time.

Now it will go and process stuff and spit out the files in the determined output folder.

Load up the game and/or smithbox and your navmesh should work as expected.

### Notes
When dragging one HKX file onto the executable, that will be the only collision for the relevant map.

Dragging multiple HKX files onto the executable will create a one navmesh file per type for the selected map.

It does not matter what the name of the HKX file is, inside the navmesh files it'll use it's own generated IDs with a spacing of 100

## Credits
- Nordgaren: [ERNavmeshGenCS](https://github.com/Nordgaren/ERNavmeshGen)
- [The12thAvenger](https://github.com/The12thAvenger): Reversing the navmesh objects to find all variables
- InfernoPlus: [JortPob](https://github.com/infernoplus/JortPob), which this tool borrows code from
- Anybody who worked on SoulsFormats and [SoulsFormatsNEXT](https://github.com/soulsmods/SoulsFormatsNEXT)
- Lord Exelot: Basic port to allow navmesh generation outside of the JortPob workflow

### Other
<img width="360" height="308" alt="image" src="https://github.com/user-attachments/assets/3fc80f7b-f2ec-4287-936f-636bcac76e82" />

# Back2Britain

<H2>Whats going on here</H2>
In the latest versions of UO 2D since July 2024, Mythic added a new compression level to the files to shrink the data partition down from 160MB to 72MB. This is maintains on the data partition within the files. 
UOFiddler and Orion both do not have the Bwt decompression built in, but CUO does. So the Bwt method has been referenced in the project as a dependency from CUO libraries, and the files are processed through their awesome work. I was able to use their code to understand this below:

The **cliloc** files, are loaded in their entirety into Bwt and written back out. This was easy, like 10 minutes effort.
The **gumpartLegacyMUL.uop** was much more complex to work out, and took me about 15 hours of effort to get right.
The file structure is as follows:
```
      uInt32 : MagicNumber
      uInt32 : Version
      uInt32 : timestamp
      Int64 : NextBlockAddress
      uInt32: Block Size
      Int32: Total file count
      ---Seek to NextBlockAddress location---
      From this point, the file is split into chunks of "Block Size". Previously 1000, now 100.
      ---Loop through chunks---
        Int32: fileCount in chunk
        Int64: Next Chunk Address
          ---Loop through files---
          Int64: File data offset
          Int32: Header length
          Int32: Size of data compressed
          Int32: Size of data decompressed
          UInt64: hash (of the filename, so the client can do lookups)
          UInt32: Data hash (not used here)
          Int16: Flag (THIS: is the decompression level. 0/1: None, 2: Normal ZLib, 3: New BWT method)
            ---Save current file position, and point buffer to file data offset---
            byte[]: Read HeaderLength
            byte[]: Read Compressed Data Length
            ---Point buffer back to last position---
          ---end loop---
        ---Seek next Chunk---
      ---end loop---
```
<H3>What really going on?</H3>
Then the data taken out of this file is decompressed with the normal ZLib method, then decompressed again using Bwt. Then we pull out the height and width of the gumps and save them into the right place. We set the flag back to 1, and do bit of number crunching to work out the new data offsets and chunk positions.
Starting positions of the data dont really matter here, because as long as the structure above is followed it all just works. The original files are zipped into a <b>backup</b> directory, and the decompressed files are written in their place.

<H2>Setup</H2>
First step, make a backup of the entire UO directory first. Its the easiest way to role back if you have issues.

Patch UO:2D by running UOPatch.exe, this will download the latest file.

Move all these files from the build into the UO Directory. Ideally I'll get this compiled into a single file executable for cleanliness.

Run **Back2Britain.exe**.
This will move all cliloc.* files into **backup** and **gumpartLegacyMUL.uop** into the backup folder. Then process the files and create new decompressed files.
![image](https://github.com/user-attachments/assets/baa1089b-7dc5-4788-9fce-aa349807bb44)

New Files will run with : **Orion/UOFiddler/CUO** (perhaps more)

https://buymeacoffee.com/adverserath
Or even just say thanks on Discord: adverserath

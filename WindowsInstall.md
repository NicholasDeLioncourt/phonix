# Installing #

Phonix requires the .NET runtime, but this should already be installed on Windows XP SP3 and all more recent versions of Windows.

Download the .zip file from the [downloads page](http://code.google.com/p/phonix/downloads/list) and save it in the location of your choice.

Right-click on the .zip file and select "Expand all...". (The exact wording of this menu option may depend on your version of Windows.) This will expand a folder containing the phonix executables, documentation, and examples.

# Running #

Open a command shell. You can this by clicking Start > Run..., then typing `cmd`. In the shell, navigate to the folder where phonix was expanded.

Once you are in same directory as phonix.exe, you can invoke phonix by simply typing:

`phonix [args]`

(Optional:) If you'd like to be able to run phonix from any command line, you can either add the directory with phonix.exe to your %PATH%, or copy phonix.exe and all of its .dll files into a directory which is already part of your %PATH% (such as `%system32%`). Explaining exactly how to do this is beyond the scope of this document, but you can easily find explanations online.
Phonix is a domain-specific language for modeling phonological transformations, be they synchronic linguistic processes or diachronic sound changes. It uses a simple notation to represent phonological rules that is very similar to the standard notation used in linguistic texts. Here's a sample from the Romanian example included in Phonix:

```
import std.features
import std.symbols

# 
# Romanian palatalization rules
#

# Palatalize velars before front vowels
rule palatalize-velars
[+cons +hi] => [-ant +dist +dr *hi] / _ [+fr]

# Palatalize dental fricatives before high front vowels
rule palatalize-dentals
[+cons +ant +cont -son] => [-ant +dist] / _ [+hi +fr]
```

Phonix supports all of the following features and more:

  * **Rules based on distinctive features, not string matching**
  * **Built-in feature set**
  * **Built-in symbol sets for both X-SAMPA and Unicode IPA**
  * **Easy extensions for adding your own features or symbols**
  * **Multi-character symbols**
  * **Diacritic symbols**
  * **Persistent rules**
  * **Debugging mode that shows you exactly what your sound changes are doing**

Instructions for installing and running Phonix can be found on the following pages:

  * [Linux](http://code.google.com/p/phonix/wiki/LinuxInstall)
  * [Windows](http://code.google.com/p/phonix/wiki/WindowsInstall)
  * [Mac](http://code.google.com/p/phonix/wiki/MacInstall)

See the [Issues list](http://code.google.com/p/phonix/issues/list) for a list of features planned in upcoming releases.

The Phonix interpreter is a command-line program designed for easy interoperability with other common scripting tools. The Phonix interpreter is written in C# and can run on any platform that has .NET, including all versions of Windows, and most flavors of Linux and MacOSX thanks to the Mono project. Phonix development is done using Mono in a Linux environment.
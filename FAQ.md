# Frequently Asked Questions #

## Questions about ##

### What is this for? ###

Phonix is designed for people who understand basic linguistics and phonology, and want a way to automatically apply sound changes. It's a "phonology modeling program" or "sound change applier".

### Who could use this? ###

Phonology students, people researching historical linguistics, linguists in general, conlangers, people with curiosity and lots of time on their hands.

### Why did you make this? ###

Because I wanted it for myself, and I was unhappy with all of the alternatives. In particular, I wanted something that used _features_ (not characters), that was easily scriptable from the command-line, and which offered a syntax similar to standard phonological notation.

### What are the alternatives? ###

There are a couple of note:

  * [sounds.exe](http://www.zompist.com/sounds.htm) by Mark Rosenfelder, a command-line program written in C. The simplest of the alternatives, which inspired many imitators.
  * [Phono](http://mypage.siu.edu/lhartman/phono.html) by Lee Hartman, a GUI program written in Visual Basic. More features than `sounds.exe`, but clunky interface.
  * [VSCA](http://muba.perlmonk.org/vsca/vsca.htm), the "Versatile Sound Change Applier", written in Perl.
  * [Zounds](http://zounds.artefact.org.nz/) by Jamie Norrish, a command-line and GUI app written in Python.

All of these have different levels of sophistication and slightly different features sets. If you have difficulty with Phonix, one of these might work better for you.

## Questions about writing sound changes ##

### How do I match any segment? ###

Use `[]`. For example, the following rule will make everything into a schwa:

```
import std.features
import std.symbols

rule everything-is-schwa
    [] => @
```

### How do I match "one or more segments"? ###

Phonix has three regexp-like operators which can accomplish this. These operators can only be used in the rule context (the part that comes after the `/`).

**To match _zero or one_ segments you enclose the optional segment in parentheses, like this: `(r)`.** To match _one or more_ segments, you enclose the segment in parentheses and add a `+` afterwards: `(r)+`
**To match _zero or more_ segments you enclose the segment in parentheses and add a `*` afterwards: `(r)*`**

For example:

```
import std.features
import std.symbols

# This turns pa => ba and pra => bra
rule zero-or-one
    p => b / _ (r) a

# This turns pra => bra, prra => brra, etc., but leaves pa unchanged
rule one-or-more
    p => b / _ (r)+ a

# This turns pa => ba, pra => bra, prra => brra, etc.
rule zero-or-more
    p => b / _ (r)* a
```

See the Phonix manual for a fuller explanation.

### How do I indicate that a rule should apply when a condition is _not_ met? ###

Use the "excluded context". This is indicated by a double-slash `//`. See the Phonix manual for a full explanation.

### Why isn't my rule working like I think it should? ###

Try using the `-d` flag to get debugging information about your rule. That should help you see what the problem is.

### I think there's a bug in Phonix. ###

[Let us know.](http://code.google.com/p/phonix/issues/entry?template=Defect%20report%20from%20user)
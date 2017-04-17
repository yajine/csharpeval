This project was initially hosted on CodePlex. Since they are shutting down, this site will continue to host the code.

# New ANTLR4 Code

ANTLR3 has been dropped due to its method of embedding code inside the grammar and so as much as possible no new enhancements will go to the AntlrParser branch. Also affecting this decision is that the ANTLR3 parser results in an infinite loop when parsing nested ternary operators at around a depth of 7.

The ANTLR4 branch now passes most tests, and avoids the infinite loop on nested ternaries, but it is still in early beta.  There may be expressions not covered by current test cases. Calls on generic methods currently fail.

# Introduction

C# does not include a built-in method to evaluate a string during runtime, like Javascript's eval() or VBScript's Eval(), although Roslyn's Scripting API now allows you to perform much of the same functionality in a similar manner.

ExpressionEvaluator is a lightweight, simple and free library capable of parsing and compiling C# expressions, statements and code blocks. 

It features an ANTLR4-based parser and generates a LINQ Expression tree which is then compiled into a delegate.

# How can I use this library?

* An [Experimental Music Compiler](http://www.windowsphone.com/en-us/store/app/experimental-music-compiler/370af565-51c8-4cd0-af6a-32d1e9a6690a) for Windows Mobile
* Dynamic data-bound XML templates
* User-defined conditional code stored in configuration files
* Customer-defined queries
* User-defined execution parameters for pluggable controls

# NuGet

Expression Evaluator is now available via [NuGet](https://www.nuget.org/packages/ExpressionEvaluator)

# Getting Started

See **Usage** and **Sample Expressions** under [Documentation](https://csharpeval.codeplex.com/documentation)

# Features

* *Compute*: Arithmetic operators: +- * / % ^
* *Compare*: Relational operators: == != < > <= >=
* *Assign*: Set values with the = operator
* *Test*: Logical Operators: ! & | (bitwise logic) and && || (short circuit logic)
* Expression grouping with parentheses ( )
* Index accessors {"[ ]"}
* Supports *dynamic* objects (including classes that implement *IDynamicMetaObjectProvider* such as *ExpandoObject*)
* Context: Register and access external types and symbols through the *TypeRegistry*
* Strings: (enclosed in '_single quotes_' and string concatenation with +)
* _true_, _false_, _null_ literals
* Declarative typing of numbers using d/f/m/l/u/ul suffixes
* Implicit conversion of numerical expressions
* Member access operator (.) for any valid expression.  Access properties, fields and methods of types, objects and expressions
* Pre-registered default types e.g. bool, int, double, float, char, string
* Supports nested function calls (x.method(y.method(z.method()), y.method2()))

# Buy me a Beer!

Programming sure is hard work.  A cold beer would really be appreciated!

* [Buy me an expensive beer! - USD 5.00](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=rupert.avery@gmail.com&lc=PH&item_name=Donate%20a%20%245%20Beer%20-%20Expression%20Evaluator&currency_code=USD&amount=5.00)
* [Buy me a nice, probably imported beer! - USD 3.00](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=rupert.avery@gmail.com&lc=PH&item_name=Donate%20a%20%243%20Beer%20-%20Expression%20Evaluator&currency_code=USD&amount=3.00)
* [Buy me a good beer! - USD 2.00](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=rupert.avery@gmail.com&lc=PH&item_name=Donate%20a%20%242%20Beer%20-%20Expression%20Evaluator&currency_code=USD&amount=2.00)
* [Buy me a cheap, but decent beer! - USD 1.00](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=rupert.avery@gmail.com&lc=PH&item_name=Donate%20a%20%241%20Beer%20-%20Expression%20Evaluator&currency_code=USD&amount=1.00)

# Disclaimer

This is *not* a fully C#-compliant compiler, and as such features may be missing and there may be bugs or discrepancies in how the parser and compiler work.  If you wish to use this library in production code do so at your own risk.

[![E-iceblue](http://www.e-iceblue.com/components/com_affiliate/banners/3ca481a23a214e4c43b56861a904e189.gif)](http://www.e-iceblue.com/Spire.PDF/Spire.PDF-for-.NET.html?aff_id=96&amp;amp;banner_id=15)

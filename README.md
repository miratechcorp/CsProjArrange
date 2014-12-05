CsProjArrange
=============

Arrange .csproj files. This project was created to use as a clean filter for .csproj files when committing using git.

Setting up in Git
-----------------

The `.gitattributes` file should be updated to the following setting for .csproj files.

    *.csproj text filter=csprojarrange

The `.git/config` file should be update to include the following section.

    [filter "csprojarrange"]
      clean = CsProjArrange

If you need to specify additional options, it looks like this.

    [filter "csprojarrange"]
      clean = "CsProjArrange --options=None"

This should cause .csproj files to always be committed to the repository with the file arranged.

Running from the command line
-----------------------------

Here is the help listing you can see by running `CsProjArrange -?`.

    Usage: CsProjArrange.exe [-?|--help] [-iINPUT|--input=INPUT] [-oOUTPUT|--output=OUTPUT] [-sSTICKY|--sticky=STICKY] [-aSORTATTRIBUTES|--attributes=SORTATTRIBUTES]
    
    Option:
      -?, --help                 Display this usage message.
      -i, --input=VALUE          Set the input file name. Standard input is the
                                   default.
      -o, --output=VALUE         Set the output file name. Standard output is the
                                   default.
      -s, --sticky=VALUE         Comma separated list of elements names which
                                   should be stuck to the top.
      -a, --attributes=VALUE     Comma separated list of attributes to sort on.
      -p, --options=VALUE        Specify options

When no command line options are specified, the following defaults take effect.

 - The input comes from standard input.
 - The output goes to standard output.
 - The list of sticky element names is:
   - `Task`
   - `PropertyGroup`
   - `ItemGroup`
   - `Target`
   - `Configuration`
   - `Platform`
   - `ProjectReference`
   - `Reference`
   - `Compile`
   - `Folder`
   - `Content`
   - `None`
   - `When`
   - `Otherwise`
 - The list of attributes just include `Include`.
 - All of the following options are selected:
   - `CombineRootElements`
     - This will combine root elements which have the same name and the same attribute values.
   - `KeepCommentWithNext`
     - This tries to keep any comments with the next node.
   - `KeepImportWithNext`
     - This is a hack to try to keep `Import` elements from moving to much and causing build failures.
   - `SortRootElements`
     - This will sort the nodes under the root element.
   - `SplitItemGroups`
     - This will split `ItemGroup` elements so that each group will only contain one type of child element.

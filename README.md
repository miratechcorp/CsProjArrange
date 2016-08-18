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
      -s, --sticky=VALUE         Comma separated list of element names which
                                   should be stuck to the top.
      -k, --keeporder=VALUE      Comma separated list of element names where
                                   children should not be sorted.
      -a, --attributes=VALUE     Comma separated list of attributes to sort on.
      -p, --options=VALUE        Specify options

When no command line options are specified, the following defaults take effect.

 - The input comes from standard input.
 - The output goes to standard output.
 - The list of sticky element names is the `[Default]` value, which expands to:
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
 - The list of elements where children should not be sorted is the `[Default]` value,
     which expands to just `Target`.
 - The list of attributes is the `[Default]` value, which expands to just `Include`.
 - All of the following options are selected:
   - `CombineRootElements`
     - This will combine root elements which have the same name and the same attribute values.
   - `KeepCommentWithNext`
     - This keeps any comments with the next node.
   - `SortRootElements`
     - This will sort the nodes under the root element.
   - `SplitItemGroups`
     - This will split `ItemGroup` elements so that each group will only contain one type of child element.

The following helper options are also available:

 - `None`
   - No options selected.
 - `All`
   - All options selected (this is the default).
 - `NoRoot`
   - Everything except `CombineRootElements` and `SortRootElements` is selected.
 - `NoSortRootElements`
   - Everything except `SortRootElements` is selected.

Option Sections
---------------

These are comments which change the options for the specified section of the projetct file. They take the following form:

 - Opening section: `<!-- Options: {options} -->`
 - Closing section: `<!-- /Options -->`

where the `{options}` are replaced with the actual options for that section (`NoSortRootElements` for example). Options sections will be moved to the bottom of the
file in the order they originally appeared; so to get a section to stick to the top, the entire file must use option sections.
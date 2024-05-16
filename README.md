# Borealis

A stateless CLI tool for tagging and grouping images and other documents.

An archive is a directory containing documents (catalogued files with special names) and a `.borealis` folder which contains cataloguing information, e.g. archive name, tags. An archive can be created in a directory with the `borealis init` command.

Files can be catalogued in an archive by placing them in the `.borealis/in` folder and running the command `borealis in`. Each document is given a document hash and the file, named with the hash, is moved to the main directory. Documents are additionally associated with a document ID, distinct from the hash; hashes are globally unique whereas IDs are only archive unique.

If `borealis in` is run with the `--info` option, then for each file ingested, execution will pause and wait for confirmation to use the current content of `.borealis/docinfo` to set the document info and associate the document with tags, sequences, etc.

Document info:
- Original file name
- Extension / MIME type
- Ingestion date
- Comment

Document commands:
- `borealis doc [doc-ref]` View document info. The document's file will be hardlinked in the `.borealis/out` directory with the correct extension.
- `borealis doc [doc-ref] open` Open document file in relevant software, e.g. image viewer.
- `borealis doc [doc-ref] delete` Delete document from archive.  

A tag is an object associated with a canonical name, a namespace, and aliases, and is identified with a tag ID.

- `borealis new tag [namespace-ref]:[canonical-name]` Create a new tag.
- `borealis tag [tag-ref]` View tag info.
- `borealis tag [tag-ref] children` View tag children.
- `borealis tag [tag-ref] child [child-tag-ref]` Add child tag.
- `borealis tag [tag-ref] unchild [child-tag-ref]` Remove child tag.
- `borealis tag [tag-ref] aliases` View tag aliases.
- `borealis tag [tag-ref] alias [alias-string]` Add tag alias.
- `borealis tag [tag-ref] unalias [alias-string]` Remove tag alias.

- `borealis doc [doc-ref] tags` View all tags on document.
- `borealis doc [doc-ref] tag [tag-ref]` Add tag to document.
- `borealis doc [doc-ref] untag [tag-ref]` Remove tag from document.

- `borealis new namespace [namespace-ref]:[canonical-name]` Create a new namespace.

On tag terminology:
- A 'name' is a string.
- A 'namespace' is a context in which names are considered.
- A 'qualified name' is an unqualified name along with the qualified name for a namespace. Names in the root namespace are qualified by definition.
- A 'fully qualified name' is a qualified name in which the namespace is fully qualified. Names in the root namespace are fully qualified by definition.

A 'reference' is a special string which can be:
- An ID written as `@[id]`.
- An object name, such as a tag name or document hash.
- A variable, such as `$CUR`. Objects can be assigned to variables with commands such as `borealis bind [obj-type] [ref]`. These are stored in `.borealis/var` and internally expanded to typed variables like `$CUR_DOC` and `$CUR_TAG`. e.g. `borealis doc $CUR tags`

A sequence is an ordered collection of documents. A document can exist in multiple sequences. Sequences are identified by a sequence ID.

- `borealis new seq [seq-title]` Create new sequence.
- `borealis seq [seq-ref]` View sequence info. The documents' files will be hardlinked in the `.borealis/out` directory.
- `borealis seq [seq-ref] add [doc-ref]` Add document to sequence.
- `borealis seq [seq-ref] remove [doc-ref]` Remove document from sequence.
- `borealis seq [seq-ref] arrange [doc-ref] [index]` Move document within sequence. '+' and '-' can be used in the index to refer to a relative position. e.g. `borealis seq $CUR arrange $CUR -1`.

The command `borealis resolve [obj-type] [ref]` can be used to find the exact ID of an object through a reference. 

The command `borealis query [query-string]` can be used to search for documents through tags and query operators such as && and ||. The result documents' files will be hardlinked in the `.borealis/out` directory.


DB structure:
- table Document
    - id: int
    - hash: string (indexed)
    - comment: string?
- table Tag
- table DocumentTag
- table SequenceDocument
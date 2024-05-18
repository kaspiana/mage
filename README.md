# Mage 🔮

Mage is designed to allow for the organisation of documents, chiefly images, through tags and document series, and for the quick retrieval of documents based on these tags and series.

## Views

Sans a GUI, document manipulation and viewing is achieved through 'views': folders containing hard links to the internal document files. Views can represent query results, a working space, the currently open files, etc.

## Taxonyms

Taxonyms generalise the notion of names and namespaces with respect to tags. Taxonyms form a tree, and each taxonym has one canonical alias, its true name, and several optional non-canonical aliases. Additionally, taxonyms can have non-canonical parents to further simplify notation. Every tag has a corresponding taxonym, making that taxonym a 'tag name', but not every taxonym has a corresponding tag; those which do not are 'tag namespaces'.

## References

A reference denotes an object like a document, tag, or view, but does not itself communicate the type of the object. There are four kinds of references:

- ID references; e.g. `/786`.
- Name references; e.g. view names, document hashes, tag names.
- Bound references, written with the `.` operator.
- View index references, which refer to a document in a view; e.g. `main/0`, `./6`.

## Working example

```bash
mage init # create an archive in the current directory
```

```bash
mage ingest # ingest files in .mage/in/
```

```bash
mage view in # list documents in the 'in' view: recently ingested documents
mage view main reflect in # reflect recently ingested documents in main view.
mage bind view main # bind main view to context
mage view in clear # clear 'in' view
```

```bash
mage doc ./0 # see information about first document in the bound view
mage doc ./0 open # open document on desktop
```

```bash
mage new tag character vriska_serket # add new tag
mage taxoym vriska_serket alias vriska # add alias to taxonym
mage doc main/0 tag vriska # add vriska tag to document
mage search "vriska" # find all documents tagged with vriska
```

```bash
mage new tag species troll
mage tag vriska imply troll # add tag implication
mage search "troll" # find all documents tagged with troll or with tags that imply troll
```

```bash
# more complex queries
mage search "troll human yuri"
mage search "kanaya_maryam OR vriska_serket"
mage search "terezi_pyrope -karkat_vantas"
mage search "-karkat_vantas OR (transfem!karkat_vantas)"
```


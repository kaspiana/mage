# Mage

Mage is designed to allow for the organisation of documents, chiefly images, through tags and document series, and for the quick retrieval of documents based on these tags and series.

## Views

Sans a GUI, document manipulation and viewing is achieved through 'views': folders containing hard links to the internal document files. Views can represent query results, a working space, the currently open files, etc.

## Taxonyms

Taxonyms are tag names and tag namespaces, and are organised into a hierarchy descending from the root taxonym. Multiple taxonyms, i.e. aliases, can be associated with a single tag, but each tag must have a canonical taxonym.

## Working example

```bash

mage ingest # ingest files in .mage/in/
mage view in # list documents in the 'in' view: recently ingested documents
mage doc in/0 # see information about first document in the 'in' view
mage view main reflect in # reflect recently ingested documents in main view.
mage view in clear # clear 'in' view

```

## Future example

```bash

mage new taxonym general # create general namespace
mage new taxonym general:character --alias char # create character namespace
mage new tag char:vriska_serket --alias vriska # create vriska tag
mage doc main/0 tags add vriska # add vriska tag to document

```
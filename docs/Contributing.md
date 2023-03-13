# Contributing

## Ignoring bulk changes

In some cases it can be useful to ignore certain revisions when using `git blame`.
From [`fantomas`](https://fsprojects.github.io/fantomas/docs/end-users/FormattingCheck.html#A-git-blame-ignore-revs-file) and [Arnout Boks](https://moxio.com/blog/ignoring-bulk-change-commits-with-git-blame/).

To configure your repo to ignore the revisions in the `.git-blame-ignore-revs` file, you can run

```shell
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

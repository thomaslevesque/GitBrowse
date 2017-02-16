# GitBrowse
A tool to open the webpage for a GitHub repository. Also works for GitLab, and maybe others.

## Usage

Open the webpage for the `origin` remote if it exists, or the first remote it finds
```
> GitBrowse
```

Open the webpage for the `foobar` remote:
```
> GitBrowse foobar
```

## Git alias

A nicer way to use it is to create a git alias. I like to call mine `hub`, because `git hub` :wink:

```
git config --global alias.hub1 '!GitBrowse $1'
```

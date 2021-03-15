# Contributing

Thank you for your interest in contributing to BossRoom!

Here are our guidelines for contributing:

* [Code of Conduct](#coc)
* [Ways to Contribute](#ways)
* [Issues and Bugs](#issue)
* [Feature Requests](#feature)
* [Improving Documentation](#docs)
* [Unity Contribution Agreement](#cla)
* [Pull Request Submission Guidelines](#submit-pr)

## <a name="coc"></a> Code of Conduct

Please help us keep BossRoom open and inclusive. Read and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

## <a name="ways"></a> Ways to Contribute

There are many ways in which you can contribute to the BossRoom.

### <a name="issue"></a> Issues and Bugs

If you find a bug in the source code, you can help us by submitting an issue to our
GitHub Repository. Even better, you can submit a Pull Request with a fix.

### <a name="feature"></a> Feature Requests

You can request a new feature by submitting an issue to our GitHub Repository.

If you would like to implement a new feature then consider what kind of change it is:

* **Major Changes** that you wish to contribute to the project should be discussed first with other developers. We will have a more formal process for this soon. For now submit your ideas as an issue.

* **Small Changes** can be directly submitted to the GitHub Repository
  as a Pull Request. See the section about [Pull Request Submission Guidelines](#submit-pr).

### <a name="docs"></a> Documentation

We accept changes and improvements to our documentation. Just submit a Pull Request with your proposed changes as described in the [Pull Request Submission Guidelines](#submit-pr).

## <a name="cla"></a> Contributor License Agreements

When you open a pull request, you will be asked to enter into Unity's License Agreement which is based on The Apache Software Foundation's contribution agreement. We allow both individual contributions and contributions made on behalf of companies. We use an open source tool called CLA assistant. If you have any questions on our CLA, please submit an issue

## <a name="submit-pr"></a> Pull Request Submission Guidelines

We use the [Gitflow Workflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) for the development of BossRoom. This means development happens on the **develop branch** and Pull Requests should be submited to it.

### Commit Message Guidelines
Always write a clear log message for your commits. One-line messages are fine for small changes, but bigger changes should look like this:

    $ git commit -m "A brief summary of the commit
    > 
    > A paragraph describing what changed and its impact."

### Line Endings Guidelines
The project is using Unix-style line endings.

Git can handle this by auto-converting CRLF line endings into LF when you add a file to the index, and vice versa when it checks out code onto your filesystem. You can turn on this functionality with the core.autocrlf setting. If you’re on a Windows machine, set it to true — this converts LF endings into CRLF when you check out code:

$ git config --global core.autocrlf true

If you’re on a Linux or macOS system that uses LF line endings, then you don’t want Git to automatically convert them when you check out files; however, if a file with CRLF endings accidentally gets introduced, then you may want Git to fix it. You can tell Git to convert CRLF to LF on commit but not the other way around by setting core.autocrlf to input:

$ git config --global core.autocrlf input


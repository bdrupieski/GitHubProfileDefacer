There are two projects in this solution.

**GitHubProfileDefacer** makes fake 
commits to a git repository following the 
pattern specified in the file "pattern.txt". 
The letter X is a placeholder and is ignored.

There are other projects that can also do this, 
like [gitfiti](https://github.com/gelstudios/gitfiti).

**GenerateFakeCommitMessages** builds fake commit
messages using a Markov Model trained on real commit
messages. It uses the GitHub API to pull down all 
commit messages for the most starred 1,000 repositories 
for a few different languages and then generates
probable commit messages based on the Markov Model
built using a given language's sample commit messages.

I wrote a blog post about generating the commit messages
[here](http://blog.briandrupieski.com/github-commit-messages-markov-chains).
There are sample messages for different languages in the post.
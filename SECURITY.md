# Security Policy

🇵🇱 [Jak zgłosić podatność (po polsku)](SECURITY.pl.md)

If you find a security vulnerability in OpenUrzednik.NET (e.g. a way to leak an API key, SSRF through a crafted URL, or a problem with validating data from government responses), **don't report it in a public issue**.

Use **Security → Report a vulnerability** in this repository instead (GitHub Private Vulnerability Reporting), or write to the repository maintainer.

Please include:

- the package (e.g. Core, Nbp) and its version,
- steps to reproduce,
- the potential impact.

We reply as soon as we can. This is a hobby open-source project, not a company with an SLA, so a reply may take a few days.

The strong-name key `OpenUrzednik.snk` in the repository is public on purpose: a strong name is an assembly identity, not a security measure ([ADR-0005](docs/adr/0005-target-frameworks.md)). It isn't a leaked secret, so please don't report it.

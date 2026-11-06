# Security Policy

🇵🇱 [Jak zgłosić podatność (po polsku)](SECURITY.pl.md)

## Supported versions

| Version | Security fixes |
| :--- | :--- |
| The latest minor version of the latest major version | ✅ |
| The previous major version | ✅ for 6 months after the next major version is released |
| Older versions, and every preview | ❌ |

A fix ships in a new patch version of the supported line (for example `1.4.2` after `1.4.1`). If you use an older minor version, update to the latest one: minor versions don't break the public API ([Versioning and support](README.md#versioning-and-support)).

## Reporting a vulnerability

If you find a security vulnerability in OpenUrzednik.NET (e.g. a way to leak an API key, SSRF through a crafted URL, or a problem with validating data from government responses), **don't report it in a public issue**.

- **Preferred:** use **Security → Report a vulnerability** in this repository (GitHub private vulnerability reporting).
- **If you can't use GitHub:** e-mail **security@ochodek.net**.

Please include:

- the package (e.g. Core, Nbp) and its version,
- steps to reproduce,
- the potential impact.

## What happens next

- We acknowledge your report **within 14 days**.
- We confirm the issue or explain why we don't consider it a vulnerability, and keep you updated while we work on it.
- We aim to publish a fixed version **within 90 days** of confirming the issue.
- We disclose the vulnerability in a **GitHub Security Advisory** for the NuGet packages, with the affected and patched version ranges, and request a CVE when it is warranted. We credit you in the advisory unless you prefer not to be named.
- We **deprecate the affected versions on nuget.org**, pointing to the patched version, so `dotnet list package --deprecated` and NuGet's tools show them.

Please keep the details private until the advisory is published.

The strong-name key `OpenUrzednik.snk` in the repository is public on purpose: a strong name is an assembly identity, not a security measure ([ADR-0005](docs/adr/0005-target-frameworks.md)). It isn't a leaked secret, so please don't report it.

<!-- CONTRIBUTING.md -->
# Contributing to RxDBDotNet

We're thrilled that you're interested in contributing to RxDBDotNet! This document provides guidelines for doing so. By participating in this project, you agree to its terms.

## Table of Contents

- [Contributing to RxDBDotNet](#contributing-to-rxdbdotnet)
  - [Table of Contents](#table-of-contents)
  - [Code of Conduct](#code-of-conduct)
  - [Getting Started](#getting-started)
  - [Development Workflow](#development-workflow)
  - [Submitting Changes](#submitting-changes)
  - [Pull Request Titles](#pull-request-titles)
  - [Coding Standards](#coding-standards)
  - [Testing](#testing)
  - [Documentation](#documentation)
  - [Community](#community)
  - [Reporting Security Issues](#reporting-security-issues)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

1. Fork the repository on GitHub.
2. Clone your fork locally.
3. Set up the development environment by following the instructions in the README.md file.

## Development Workflow

We use a trunk-based development model:

1. Create a new branch for your feature or bug fix. Keep it short-lived.
2. Make your changes in that branch.
3. Write or update tests as needed.
4. Update documentation to reflect your changes.
5. Ensure all tests pass and the build is successful.
6. Create a pull request to merge your changes into the main branch.

## Submitting Changes

1. Push your changes to your fork on GitHub.
2. Create a pull request from your fork to the main RxDBDotNet repository.
3. Provide a clear title for your pull request that follows the [Conventional Commits](https://www.conventionalcommits.org/) syntax.
4. Provide a clear description of the changes in your pull request.
5. Link any relevant issues in the pull request description.
6. Be prepared to address feedback and make additional changes if requested.

Note: We use squash merges for all pull requests. The pull request title will be used as the commit message in the main branch, so it's crucial that it follows the Conventional Commits syntax.

## Pull Request Titles

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification for our pull request titles. This leads to more readable messages that are easy to follow when looking through the project history.

## Coding Standards

- Follow the existing code style in the project.
- Use meaningful variable and function names.
- Write clear comments for complex logic.
- Please make sure your code works with nullable reference types enabled.
- Use C# 8.0+ features where appropriate.

## Testing

- Write unit tests for new functionality.
- Please make sure all existing tests pass before submitting your changes.
- Aim for high test coverage, especially for critical paths.

## Documentation

- Update the README.md if you change functionality.
- Document public APIs using XML comments.
- Keep documentation clear, concise, and up-to-date.

## Community

- Be respectful and constructive in discussions.
- Help others who have questions.
- Share your knowledge and experiences.

## Reporting Security Issues

If you discover a security vulnerability, please do not raise it in a public issue. Instead, you can report it privately using the instructions in our [Security Policy](./SECURITY.md). This ensures we can address the issue responsibly before publicly disclosing it.

Thank you for contributing to RxDBDotNet!

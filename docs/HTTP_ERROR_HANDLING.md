# HTTP Error Handling

The API centralizes RFC 7807 problem responses and expected exception-to-HTTP mappings.

Controllers do not construct `ProblemDetails` directly and do not independently translate
optimistic-concurrency exceptions.

## Problem Response Factory

`ApiProblems` owns stable HTTP problem titles and descriptions used by:

- controllers;
- authentication endpoints;
- administration authorization filters;
- local-authentication availability filters;
- the centralized exception handler.

This prevents different endpoints from returning conflicting descriptions for the same
failure category.

## Central Exception Handler

`ApiExceptionHandler` maps expected request and infrastructure exceptions.

Current mappings are:

| Exception | HTTP status | Meaning |
|---|---:|---|
| `ArgumentException` | 400 | Request data violates a domain or value-object contract |
| `IdentityConcurrencyException` | 409 | A stale optimistic-concurrency version was supplied |
| `DatabaseRouteException` | 503 | The required identity data route is unavailable |
| `PostgreSqlStorageException` | 503 | The identity data store is unavailable |

Unexpected exceptions are not converted by this handler and remain subject to the host's
standard unhandled-exception path.

Request cancellation is not converted into a business or HTTP problem response when the
request-aborted token is already cancelled.

## Information Disclosure

Expected problem responses use stable client-safe titles and descriptions.

Raw exception messages, stack traces, connection information, secret references, tokens,
passwords, and storage internals are not returned to clients.

Expected technical dependency exceptions mapped to `5xx` responses are logged server-side.

## Controller Responsibilities

Controllers remain responsible for successful response shapes and explicit business
branches such as authentication rejection or resource absence.

They do not own infrastructure exception translation or optimistic-concurrency exception
translation.

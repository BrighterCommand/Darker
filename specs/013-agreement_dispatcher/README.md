# Agreement Dispatcher

**Spec ID:** 013-agreement_dispatcher
**Created:** 2026-07-14
**Status:** Requirements drafted — awaiting review/approval
**Source:** [Issue #349](https://github.com/BrighterCommand/Darker/issues/349) — Create an Agreement Dispatcher based on the Query and Context

## Overview

Brighter supports not only type-based routing, which matches a handler to a specific request
type, but also an Agreement Dispatcher, which allows the matching to be done on any part of the
context given to the command dispatcher — that is, the request and the context. See
[Agreement Dispatcher](https://brightercommand.gitbook.io/paramore-brighter-documentation/brighter-request-handlers-and-middleware-pipelines/agreementdispatcher)
in Brighter's documentation, and Brighter's `docs/adr/0031-support-agreement-dispatcher.md` for
the design this mirrors.

Brighter implements this via a `Func` that maps between request and handler in the
`SubscriberRegistry`. If the user does not supply this, the default matches on type, but the
user can supply a `Func` that routes differently. Brighter's Subscriber Registry stores a set of
"observers" for a request, each an implementation of `Func` that can produce a handler for that
type, since `CommandProcessor.Publish` can have multiple matches.

Darker should mirror this design: allow a different `QueryHandler` pipeline to be chosen
depending on both the `Query` itself and the `QueryContext`. A typical usage is date-based —
queries before a certain date use one shape, and after that date use another, following a
decision to change how a particular metric is provided.

## Status Checklist

- [x] Requirements (`/spec:requirements`) — drafted, pending approval
- [ ] Design / ADR (`/spec:design`)
- [ ] Adversarial Review
- [ ] Task Breakdown (`/spec:tasks`)
- [ ] Implementation (`/spec:implement`)

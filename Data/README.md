# Data Folder – Database Design & Scripts

This folder is reserved for **database-related work** for this assignment.

You are expected to design and implement the **SQLite database** required to support the application, based on the requirements described in the take‑home assignment.

---

## What You Are Expected to Do

You should use this folder to store:

- SQLite database file (`.db`) created as part of your solution
- SQL scripts used to:
  - Create tables
  - Define relationships
  - Implement soft-delete behavior
  - Create views or reusable queries (if applicable)
- Any database-related documentation or notes

You may choose to:
- Create the database using Entity Framework Core migrations
- Create the database using raw SQL scripts
- Use a combination of both approaches

---

## Important Constraints

- **Hard deletion is NOT allowed**
- Soft deletion must be implemented by design
- No soft-delete or active/inactive fields are provided upfront
- You must design how soft delete works and explain it in your README

Soft-deleted records must:
- Remain in the database
- Be excluded from standard application queries
- Be available for audit or troubleshooting scenarios

---

## What Is NOT Provided

- No pre-created SQLite database file
- No tables, schema, or seed data
- No example SQL scripts

All database structure and logic must be designed by you.

---

## Documentation Requirement

In your root `README.md`, please explain:

- How your database schema is structured
- How soft deletion is implemented
- Where filtering logic is enforced (database vs application)
- Any trade-offs or assumptions you made

There is no single correct solution — we are evaluating your reasoning and design decisions.

---
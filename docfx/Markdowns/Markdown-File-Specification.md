# 1. File Structure Overview

Every Work Instruction Markdown file consists of:

```text
1. YAML Front Matter
2. Optional H1 Title
3. Sequential content:
   - Part tables
   - Step sections (##)
```

---

# 2. Core Principles

1. **Markdown is for humans**
2. **HTML comments are for the system**
3. **Order in file = execution order**
4. **Only a small set of headings have special meaning**
5. **Everything else is preserved as-is**

---

# 3. YAML Front Matter

Must appear at the top of the file.

```yaml
---
title: Bicycle Drivetrain
version: 1.2
shouldGenerateQrCode: false
collectsProducedPartSerialNumber: true 
producedPartName: Widget A
producedPartNumber: W-123

associatedProducts:
  - Bicycle

audit:
  createdBy: sthyen
  createdOn: 2026-04-20T14:32:00Z
  modifiedBy: sthyen
  modifiedOn: 2026-04-21T09:10:00Z
---
```

---

## Required Fields

| Field              | Type   |
|--------------------|--------|
| `title`            | string |
| `version`          | number |
| `producedPartName` | string |

---

## Optional Fields

| Field                              |
|------------------------------------|
| `shouldGenerateQrCode`             |
| `collectsProducedPartSerialNumber` |
| `producedPartNumber`               |
| `associatedProducts`               |
| `audit`                            |

---
# 4. Document Title

```md
# Bicycle Drivetrain
```

* Optional but recommended
* Ignored by parser (fallback to YAML `title` if missing)

---

# 5. Part Nodes
## 5.1 Required Format

```md
<!-- MESS:PART -->

| Part Name       | Part Number |
|-----------------|-------------|
| Housing         | HS-100      |
| Random Part 2.0 |             |

```
Which looks like...

<!-- MESS:PART -->

| Part Name       | Part Number |
|-----------------|-------------|
| Housing         | HS-100      |
| Random Part 2.0 |             |


---

## 5.2 Rules

### Must include:

* A `<!-- MESS:PART -->` comment block
* A table with the exact headers: `Part Name` and `Part Number`

---

### Positioning

* A PartNode applies **at the exact location it appears**
* It is part of the linear workflow

---

# 6. Step Nodes

---

## 6. Step Definition

A StepNode is defined by:

```md
## Install Bottom Bracket
```

---

## 6.2 Rules

* Every `##` heading = **StepNode**
* Step order = file order
* Step name = heading text

---

# 7. Step Content Structure

---

## 7.1 Body

All content **after `##` and before any `###` sections**:

```md
## Install Bottom Bracket

This is the step body.
```

---

## 7.2 Allowed Freeform Content

Inside body, you may use:

* Paragraphs
* Lists
* Bold/italic
* Inline images
* Arbitrary `###` headings (see below)

---

# 8. Step Subsections (`###`)

Only specific `###` headings are interpreted.

---

## 8.1 Special Sections

### `### Details`

```md
### Details
Detailed instructions here
```

→ Maps to: `DetailedBody`

---

### `### Secondary Media`

```md
### Secondary Media
![Torque](media/torque.png)
```

→ Maps to: `SecondaryMedia`

---

## 8.2 All Other `###` Headings

```md
### Preparation
### Pass Criteria
### Fail Criteria
```

Treated as **normal body content**

---

# 9. Media Rules

---

## 9.1 Primary Media

Any image:

```md
![Bracket](media/bracket.png)
```

that is:

* not inside `### Secondary Media`

→ goes to `PrimaryMedia`

---

## 9.2 Secondary Media

Only images inside:

```md
### Secondary Media
```

→ go to `SecondaryMedia`

---

# 10. Node Ordering

---

## Linear Execution Model

The document is parsed top-to-bottom:

```text
[0] PartNode
[1] StepNode
[2] StepNode
[3] PartNode
```

---

## Important

* No nesting
* No hierarchy between nodes
* Order is everything

---

# 11. Validation Rules

---

## Must Validate

### 1. Every Part must have a comment block and table

```md
<!-- MESS:PART -->   required
| Part Name       | Part Number |
|-----------------|-------------|
| Housing         | HS-100      |
| Random Part 2.0 |             |
```

---

### 2. Every Step must have a `##` heading

---

### 3. No malformed comment blocks

```md
<!-- MESS:PART ... -->   ✅
<!-- PART -->            ❌ invalid
```

---

### 4. Known section headers must be exact

```md
### Details           ✅
### Detail            ❌
```

---

# 12. Disallowed Patterns

---

### ❌ Nested Steps

```md
## Step A
### Step B   ❌ invalid
```

---

### ❌ Using headings for Parts

```md
## Part: Housing   ❌ not allowed
```

---

### ❌ JSON blobs

````md
```json
{ "partId": 42 }
````

````

# 14. Example

```md
---
title: Bicycle Drivetrain
version: 1.2
shouldGenerateQrCode: false
producedPartName: Widget A

associatedProducts:
  - Bicycle

audit:
  createdBy: sthyen
  createdOn: 2026-04-20T14:32:00Z
  modifiedBy: sthyen
  modifiedOn: 2026-04-21T09:10:00Z
---

# Bicycle Drivetrain

<!-- MESS:PART-->
| Part Name       | Part Number |
|-----------------|-------------|
| Housing         | HS-100      |
| Random Part 2.0 |             |

## Install Bottom Bracket

Install the bottom bracket into the frame.

### Preparation
Clean threads before installation.

### Pass Criteria
- Smooth rotation
- Fully seated

### Fail Criteria
- Resistance
- Cross-threading

### Details
Ensure proper alignment.

![Bracket](media/bracket.png)

### Secondary Media
![Torque](media/torque.png)
```
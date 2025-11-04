# 🧩 ReadmeSync

> ⚙️ A lightweight C# tool to automatically generate or update README / ROADMAP files  
> based on your project’s actual source code structure.

---

## 🚀 Overview

**ReadmeSync** scans your project for `.cs` files and creates a structured overview  
of your namespaces, classes, public methods, and `// TODO` comments.  

It can merge this information **into an existing README or ROADMAP**, keeping your  
manual section intact while automatically updating the technical summary below  
a special marker:

```markdown
<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

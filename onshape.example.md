# Local Onshape targets

Copy this file to `onshape.local.md` (ignored by Git) and replace the placeholders with your own workspace links and IDs. Do not put credentials in either file.

Document ID: `<document-id>`
Workspace ID: `<workspace-id>`

| Local file / role | Element ID | Workspace URL |
|---|---|---|
| `fin_generator.fs` / Feature Studio | `<generator-element-id>` | `https://cad.onshape.com/documents/<document-id>/w/<workspace-id>/e/<generator-element-id>` |
| `fin_mold.fs` / Feature Studio | `<mold-element-id>` | `https://cad.onshape.com/documents/<document-id>/w/<workspace-id>/e/<mold-element-id>` |
| Active Part Studio | `<part-studio-element-id>` | `https://cad.onshape.com/documents/<document-id>/w/<workspace-id>/e/<part-studio-element-id>` |

These mappings identify upload and inspection targets; they do not automatically synchronize files or configure the MCP test tools' server-side targets. Read the current Feature Studio before an authorized upload to check for Onshape-side edits.

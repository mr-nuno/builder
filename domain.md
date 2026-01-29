# Domain Model

## TodoItem
- Title (string): Max 200 tecken, Required
- Description (string): Max 1000 tecken
- IsCompleted (bool): Default false
- Priority (int): Range 1-5, Default 1
- DueDate (datetime?)

## Tag
- Name (string): Unique, Required
- ColorHex (string): Format #RRGGBB
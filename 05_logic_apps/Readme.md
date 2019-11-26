# Tips
 ### Get first element from array:
 ```js
 @body('Describe_Image_Content')?['description']?['captions']?[0]?['text']
 ```
 `Describe_Image_Content` is an action that. It returns an output with a property `description`. This in turn has a property `captions` which ia an array of elements. We take the `0`-th element and retrieve its `text` property.

### Embed an image into HTML
```js
@concat('<img src=''data:image/png;base64,', body('Get_blob_content')?['$content'], '''/>')
```
Action `Get_blob_content` returns an object with a property `$content`. The property represent an image encoded as Base64.

### Embed a link into HTML
```js
@concat('<a href=''', body('Create_SAS_URI_by_path')?['WebUrl'], '''>click<a>')
```

Action `Create_SAS_URI_by_path` returns a URL string as `WebUrl` property.

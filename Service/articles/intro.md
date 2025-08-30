---
title: "How to use the Field microservice?"
output: html_document
---

Typical Usage
===
1. Upload a new Field using the `Post` web api method.
2. Call the `Get` method with the identifier of the uploaded Field as argument. 
The return Json object contains the Field description.
3. Optionally send a `Delete` request with the identifier of the Field in order to delete the Field if you do not 
want to keep the Field uploaded on the microservice.



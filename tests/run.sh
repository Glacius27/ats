#!/bin/bash
newman run ats.postman_collection.json \
  --env-var "Resume=$(pwd)/resume.pdf"
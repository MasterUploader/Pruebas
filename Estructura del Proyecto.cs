"configurations": {
  "production": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.prod.ts"
      }
    ],
    "optimization": true,
    "outputHashing": "all",
    "sourceMap": false
  },
  "dev": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.dev.ts"
      }
    ],
    "sourceMap": true,
    "optimization": false
  },
  "uat": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.uat.ts"
      }
    ],
    "sourceMap": true,
    "optimization": true
  }
}




"scripts": {
  "start": "ng serve",
  "build:dev": "ng build --configuration=dev",
  "build:uat": "ng build --configuration=uat",
  "build:prod": "ng build --configuration=production"
  }




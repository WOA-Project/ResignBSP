# ResignBSP

Tool meant to simplify resigning tasks of various subrepos.

It will automatically handle resigning PE files as well, and has built in options to resign files separately, or work more efficiently with git repositories.

## Usage

- Signing an individual file:

```
ResignBSP.exe KMDF.pfx UMDF.pfx "UltraSecureCertPassword" F:\LifeInsurance.dll
```

- Signing a set of files:

```
ResignBSP.exe KMDF.pfx UMDF.pfx "UltraSecureCertPassword" F:\ImportantDocuments
```

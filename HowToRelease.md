# How to Release a New Version to Microsoft Store

This document is for code owners.

## 1. Update Version Number

Update the version in **RunCat365/RunCat365.csproj**:
- Update `<Version>X.Y.Z</Version>` (3-digit format)

## 2. Build the App

1. Open the solution in Visual Studio
2. Verify the build succeeds in Release configuration

## 3. Create the Package

1. In Visual Studio, right-click on the **RunCat365** project
2. Select **Publish** > **Create App Packages**
3. Choose **Microsoft Store as MSIX package** and sign in with your Microsoft account
4. Select the existing app "RunCat 365"
5. Configure package settings (x64, arm64, etc.)
6. Generate the `.msixupload` file

## 4. Submit to Partner Center

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard)
2. Navigate to **Apps and games** > **RunCat 365**
3. Click **Start a new submission**
4. Update the following sections:
   - **Packages**: Upload the generated `.msixupload` file
   - **Store listings**: Update description/screenshots if needed
   - **Release notes**: Describe changes in this version
5. Click **Submit to the Store**

## 5. Wait for Certification

- Certification typically takes 1-3 business days
- If issues are found, fix them and resubmit

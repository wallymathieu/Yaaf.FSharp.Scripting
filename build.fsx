// ----------------------------------------------------------------------------
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
// ----------------------------------------------------------------------------
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System
open System.IO
open Fake.Core
open Fake.Core.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.IO
open Fake.Tools

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Yaaf.FSharp.Scripting"

// List of author names (for NuGet package)
let authors = [ "Matthias Dittrich" ]

// Tags for your project (for NuGet package)
let tags = "fsharp scripting compiler host"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A helper library to easily add F# scripts to your application."

// Default target configuration
let configuration = "Release"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "matthid"
let gitHome = sprintf "%s/%s" "https://github.com" gitOwner

// The name of the project on GitHub
let gitName = "Yaaf.FSharp.Scripting"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.githubusercontent.com/matthid"
let release = LoadReleaseNotes "doc/ReleaseNotes.md"
// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|Shproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | f when f.EndsWith("shproj") -> Shproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

let (|FsFile|CsFile|) (codeFileName:string) =
    match codeFileName with
    | f when f.EndsWith(".fsx") -> FsFile
    | f when f.EndsWith(".fs") -> FsFile
    | f when f.EndsWith(".cs") -> CsFile
    | _                           -> failwith (sprintf "Code file %s not supported. Unknown code type." codeFileName)


// Generate assembly info files with the right version & up-to-date information
Target.Create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion
          Attribute.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

// --------------------------------------------------------------------------------------
// Build library & test project

let vsProjProps = 
#if MONO
    [ ("DefineConstants","MONO"); ("Configuration", configuration) ]
#else
    [ ("Configuration", configuration); ("Platform", "Any CPU") ]
#endif
let solutionFile = "src" </> "Yaaf.FSharp.Scripting.sln"

Target.Create "RestorePackages" (fun _ ->
    !! solutionFile
    |> MSBuildReleaseExt "" vsProjProps "Restore"
    |> ignore
)

Target.Create "Clean" (fun _ ->
    !! solutionFile |> MSBuildReleaseExt "" vsProjProps "Clean" |> ignore
    CleanDirs ["bin"; "temp"; "doc/output"]
)

Target.Create "Build" (fun _ ->
    !! solutionFile
    |> MSBuildReleaseExt "" vsProjProps "Rebuild"
    |> ignore
)

Target.Create "RunNetCoreTests" (fun _ ->
  DotNetCli.RunCommand id ("tests/SynVer.Tests/bin/"+configuration+"/netcoreapp2.0/SynVer.Tests.dll --summary")
)

Target.Create "RunTests" Target.DoNothing
Target.Create "All" Target.DoNothing
// Define your FAKE targets here
open Fake.Core.TargetOperators

"RunNetCoreTests" ==> "RunTests"

"Clean"
  ==> "RestorePackages"
//  ==> "CreateDebugFiles"
//  ==> "SetVersions"
//  ==> "ReadyForBuild"

// Dependencies
"Build" 
  //==> "CopyToRelease"
  //==> "CreateReleaseSymbolFiles"
  //==> "NuGetPack"
  //==> "AllDocs"
  ==> "All"

Target.RunOrDefault "All"

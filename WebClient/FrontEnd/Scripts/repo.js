"use strict";

let repoNameObject;
let branchesDiv;
let commitsDiv;
let repoName;
let commitSet;
let fileStructure;
let downloadButton;
let filesDiv;
let filesPathObj;
let selectedCommit;
let rootDirectory;
let homeButton;
let fileContentsLabel;
let fileContents;
let editor;
document.body.onload = () => {
    repoNameObject = document.getElementById("repoName");
    branchesDiv = document.getElementById("branchesDiv");
    commitsDiv = document.getElementById("commitsDiv");
    downloadButton = document.getElementById("downloadButton");
    filesDiv = document.getElementById("filesDiv");
    filesPathObj = document.getElementById("filesPath");
    homeButton = document.getElementById("returnHomeButton");
    fileContentsLabel = document.getElementById("fileContentsLabel");
    fileContents = document.getElementById("fileContents");
    homeButton.onclick = () => {window.location.href = "../Pages/main.html";};
    let urlParams = new URLSearchParams(window.location.search);
    if(urlParams.get("repo") === null || urlParams.get("repo") === undefined)
    {
        window.location.href = "../Pages/main.html";
        return;
    }
    repoName = urlParams.get("repo");
    repoNameObject.innerText = repoName;
    JSONRequest("GET", "http://localhost:56854/api/pipe/" + repoName + "/branches", "", (a) => {loadBranches(JSON.parse(a));}, (a) => {window.location.href = "../Pages/main.html";});
};

//window.onload = () => {
//    editor = ace.edit("editorDiv");
//    editor.setTheme("ace/theme/monokai");
//    editor.session.setMode("ace/mode/javascript");
//};

function loadBranches(branches)
{
    for(let key in branches)
    {
        let element = document.createElement("input");
        element.type = "button";
        element.value = key;
        element.onclick = () => selectBranch(key);
        branchesDiv.appendChild(element);
        branchesDiv.appendChild(document.createElement("div"));
        //branchesDiv.innerHTML += "<input type=\"button\" value=" + key +" onclick=\"selectBranch(this)\"></input> <div></div>";
    }
}

function getZip()
{
    JSONRequest("GET", "http://localhost:56854/api/pipe/" + repoName + "/" + selectedCommit, "", (a) => {window.location.href = a;}, (a) => {});
}

function selectBranch(branch)
{
    JSONRequest("GET", "http://localhost:56854/api/pipe/" + repoName + "?branch=" + branch, "", (a) => {loadCommits(JSON.parse(a));}, (a) => {});
}

function loadCommits(commits)
{
    commitSet = commits;
    let newestCommitID;
    let newestCommit;
    let commitsWhichAreNotNewest = new Set();
    for(let key in commits)
    {
        commitsWhichAreNotNewest.add(commits[key].Parent);
    }

    for(let key in commits)
    {
        if(!commitsWhichAreNotNewest.has(key * 1))
        {
            newestCommitID = key;
            newestCommit = commits[key];
            break;
        }
    }
    
    removeChildren(commitsDiv);

    for(let key in commits)
    {
        let element = document.createElement("input");
        element.type = "button";
        element.value = newestCommitID + ":\t" + newestCommit.Message;
        let newID = newestCommitID;
        element.onclick = () => { selectCommit(newID); };
        commitsDiv.appendChild(element);
        commitsDiv.appendChild(document.createElement("div"));
        newestCommitID = newestCommit.Parent;
        newestCommit = commits[newestCommitID];
    }
}

function selectCommit(commitId)
{
    fileContentsLabel.innerText = "";
    fileContents.innerText = "";
    downloadButton.disabled = false;
    selectedCommit = commitId;
    fileStructure = JSON.parse(getFiles(commitId));
    rootDirectory = fileStructure;
    selectFolder(fileStructure);
}

function removeChildren(node)
{
    while(node.firstChild)
    {
        node.removeChild(node.firstChild);
    }
}

function selectFolder(folder)
{
    
    removeChildren(filesDiv);
    let element1 = document.createElement("input");
    element1.type = "button";
    element1.value = "Root Directory";
    element1.onclick = () => { fileStructure = rootDirectory; selectFolder(fileStructure); };
    filesDiv.appendChild(element1);
    filesDiv.appendChild(document.createElement("div"));
    for(let subDir of folder.Folders)
    {
        let element = document.createElement("input");
        element.type = "button";
        let array = subDir.Path.split("\\");
        element.value = array[array.length - 1];
        element.onclick = () => { selectFolder(subDir); };
        filesDiv.appendChild(element);
        filesDiv.appendChild(document.createElement("div"));
    }
    for(let file of folder.Files)
    {
        let element = document.createElement("input");
        element.type = "button";
        let array = file.Path.split("\\");
        element.value = array[array.length - 1];
        element.onclick = () => { selectFile(file); };
        filesDiv.appendChild(element);
        filesDiv.appendChild(document.createElement("div"));
    }
}

function selectFile(file)
{
    fileContentsLabel.innerText = file.Path.slice(1, file.Path.length);
    //fileContents.innerText = file.Contents;
    //fileContents.innerText = fileContents.innerText.replace(/\" /g, '\ "');
    fileContents.innerHTML = file.Contents.replace(/&/g, "&amp;").replace(/ /g, "&nbsp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/\n/g, "<br>");
}

function getFiles(commitId)
{
    let currentCommit = commitSet[commitId];
    if(currentCommit.Parent == 0)
    {
        return mergeDiffs("", JSON.parse(currentCommit.Diffs));
    }
    else
    {
        return mergeDiffs(getFiles(currentCommit.Parent), JSON.parse(currentCommit.Diffs));
    }
}

function mergeDiffs(left, diffs)
{
    let i;
    for (i = 0; i < diffs.length; i++)
    {
        left = left.slice(0, diffs[i].Position) + diffs[i].ContentToAdd + left.slice(diffs[i].Position + diffs[i].NumberToRemove, left.length)//.Insert(diffs[i].Position, diffs[i].ContentToAdd);
    }
   return left;
}

function JSONRequest(requestType, url, body, runMeSuccess, runMeFailure) {
    let request = new XMLHttpRequest();
    request.open(requestType, url, true);
    request.setRequestHeader("Content-Type", "application/json");
    request.onreadystatechange = () =>{
        if(request.readyState === 4){
            //callback
            if(request.status >= 200 && request.status < 300)
            {
                runMeSuccess(request.responseText);
            }
            else
            {
                runMeFailure(request.responseText);
            }
        }
    };
    if(requestType === "GET")
    {
        request.send();
    }
    else
    {
        request.send(body);
    }
}
import json
import sys
import os
import re

def FailCommit(reason):
    print("Oh no! Bad commit! "+reason)
    sys.exit(1)

def GetStagedFileContent(filePath):
    cmd = 'git show :' + filePath
    stream = os.popen(cmd)
    return stream.read()

def GetLFSStagedFileContent(filePath):
    cmd = f"git cat-file blob :{filePath} | git lfs smudge"
    stream = os.popen(cmd)
    return stream.read()

output = GetStagedFileContent('Packages/manifest.json')
packages = json.loads(output)['dependencies']
if ('com.unity.multiplayer.virtualprojects' in packages):
    FailCommit("Virtual projects in packages")
if ('github' in packages['com.unity.multiplayer.tools'].lower()):
    FailCommit("Tools using github package")

content = GetLFSStagedFileContent('ProjectSettings/ProjectSettings.asset')
res = re.search(r'.*cloudProjectId:.*\w+\s*\n', content) != None
if res:
    FailCommit("Detected cloud project id in project settings")

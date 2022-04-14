import json
import sys
import os

def FailCommit(reason):
    print("Oh no! bad package about to be commited! "+reason)
    sys.exit(1)

cmd = 'git show :Packages/manifest.json'

stream = os.popen(cmd)
output = stream.read()

packages = json.loads(output)['dependencies']
if ('com.unity.multiplayer.virtualprojects' in packages):
    FailCommit("virtual projects in packages")
if ('github' in packages['com.unity.multiplayer.tools'].lower()):
    FailCommit("tools using github package")



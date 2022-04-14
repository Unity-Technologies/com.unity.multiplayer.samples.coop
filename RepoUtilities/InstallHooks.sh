echo "starting"
cp pre-commit.toInstall ../.git/hooks/pre-commit
chmod +x ../.git/hooks/pre-commit
# python3 is necessary to run these hooks, so testing if python is installed
echo "checking if python is installed, if the following errors out, please install python3"

python3 -c "print('done installing')"

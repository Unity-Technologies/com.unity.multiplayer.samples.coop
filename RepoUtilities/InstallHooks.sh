echo "starting"
cp pre-commit.toInstall ../.git/hooks/pre-commit
chmod +x ../.git/hooks/pre-commit
echo "checking if python is installed, if the following errors out, please install python3"

python3 -c "print('done installing')"


#!/bin/bash

# Task 11.4 - Run BucHunt automated tests

cd "$(dirname "$0")" || exit 1

echo "=================================="
echo " BucHunt Automated Workflow Tests"
echo "=================================="

echo ""
echo "Running .NET tests..."
dotnet test BucHunt.Tests/BucHunt.Tests.csproj
dotnet_result=$?

echo ""
echo "Running JavaScript tests..."
npm ci

if [ $? -eq 0 ]; then
    npm test
    javascript_result=$?
else
    javascript_result=1
fi

echo ""
echo "=================================="
echo "           TEST RESULTS"
echo "=================================="

if [ $dotnet_result -eq 0 ]; then
    echo ".NET Tests: PASSED"
else
    echo ".NET Tests: FAILED"
fi

if [ $javascript_result -eq 0 ]; then
    echo "JavaScript Tests: PASSED"
else
    echo "JavaScript Tests: FAILED"
fi

echo "=================================="

if [ $dotnet_result -eq 0 ] && [ $javascript_result -eq 0 ]; then
    echo "RESULT: ALL TESTS PASSED"
    exit 0
else
    echo "RESULT: TESTS FAILED"
    exit 1
fi

$value = get-date -Format "yyyyMMdd-HHmmss"
Write-Host "##vso[task.setvariable variable=deploymentDateTimeStamp;]$value" 

$storageAccountName = "laurisdeployments"
$rsg = "lauris-shared"

$accountKeys = Get-AzStorageAccountKey -ResourceGroupName $rsg -Name $storageAccountName
$storageContext = New-AzStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $accountKeys[0].Value
$now = Get-Date
$value = New-AzStorageContainerSASToken -Name builds -Context $storageContext -Permission r -StartTime $now.AddHours(-1) -ExpiryTime $now.AddDays(30)
Write-Host "##vso[task.setvariable variable=deploymentBlobContainerSasToken;]$value"

$container = Get-AzStorageContainer -Name builds -Context $storageContext
$uri = $container.CloudBlobContainer.Uri

$buildNumber = $env:Build_BuildNumber
$sourceBranchName = $env:Build_SourceBranchName
Write-Host "##vso[task.setvariable variable=deploymentBlobContainerUri;]$uri/$sourceBranchName/$buildNumber"
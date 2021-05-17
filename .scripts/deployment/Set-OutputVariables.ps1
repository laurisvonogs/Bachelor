param(
    [Parameter(Mandatory = $True)]
    $resourceGroup,
    [Parameter(Mandatory = $True)]
    $templateName
    )

$deployments = Get-AzResourceGroupDeployment -ResourceGroupName $resourceGroup
$lastDeployment = ($deployments | Where-Object { $_.DeploymentName -like $templateName -and $_.ProvisioningState -eq 'Succeeded' }) | sort -Property Timestamp -Descending | Select -First 1
ForEach ($Key in $LastDeployment.Outputs.Keys) {
    Write-Host "Set environment variable $Key to $($LastDeployment.Outputs.Item($Key).Value)"
    Write-Host "##vso[task.setvariable variable=$Key;]$($LastDeployment.Outputs.Item($Key).Value)"
}
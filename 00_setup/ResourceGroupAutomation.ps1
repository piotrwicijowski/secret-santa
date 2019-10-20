$connectionName = "AzureRunAsConnection"
try
{
    $servicePrincipalConnection = Get-AutomationConnection -Name $connectionName
    "Logging in to Azure..."
    Connect-AzAccount `
        -ServicePrincipal `
        -TenantId $servicePrincipalConnection.TenantId `
        -ApplicationId $servicePrincipalConnection.ApplicationId `
        -CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint 
}
catch {
    if (!$servicePrincipalConnection)
    {
        $ErrorMessage = "Connection $connectionName not found."
        throw $ErrorMessage
    } else{
        Write-Error -Message $_.Exception
        throw $_.Exception
    }
}
Get-AzResourceGroup |
    Where-Object { $_.Tags -ne $null -and $_.Tags.ggcaccesscontrol -ne "granted" } | 
    Where-Object {$_.ResourceGroupName.ToLower().StartsWith('ggc')} | 
    Foreach-Object {
        $groupName = $_.ResourceGroupName
        $groupCreation = Get-AzLog -ResourceGroup $groupName |
            Where-Object {$_.OperationName.Value -eq 'Microsoft.Resources/subscriptions/resourceGroups/write'} |
            Where-Object {$_.Status.Value -eq 'Succeeded'} |
            Where-Object {$_.subStatus.Value -eq 'Created'} |
            Select -Last 1
        if($groupCreation -ne $null){
            $creatorObjectId = $groupCreation.claims.Content["http://schemas.microsoft.com/identity/claims/objectidentifier"]
            New-AzRoleAssignment -ObjectId $creatorObjectId `
                -RoleDefinitionName "Contributor" `
                -ResourceGroupName $groupName
            Set-AzResourceGroup -Name $groupName -Tag @{ ggcaccesscontrol="granted"}
        }

    }



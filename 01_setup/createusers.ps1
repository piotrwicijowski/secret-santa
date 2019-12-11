$userList = @(
    @{DisplayName = ""           ; MailNickName = ""           ; Password = ""},
);
$tenantName = "";
$resourceGroupPrefix = "";
$defaultLocation = "South Central US"
$AadGroupID = ""
$userList | Foreach-Object {
    $user = $_;
    $PasswordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile
    $PasswordProfile.Password = $user.Password
    $PasswordProfile.EnforceChangePasswordPolicy = 1
    $PasswordProfile.ForceChangePasswordNextLogin = 1
    $UserPrincipalName = "$($user.MailNickName)@$($tenantName)"

    New-AzureADUser -DisplayName $user.DisplayName `
        -AccountEnabled $true `
        -MailNickName $user.MailNickName `
        -UserPrincipalName $UserPrincipalName `
        -PasswordProfile $PasswordProfile ` | Out-Null

    $ADuser = Get-AzADUser -UserPrincipalName $UserPrincipalName
    Add-AzADGroupMember -TargetGroupObjectId $AadGroupID -MemberObjectId $ADuser.Id

    $groupName = "$resourceGroupPrefix$($user.MailNickName)" 
    New-AzResourceGroup -Name $groupName -Location $defaultLocation

    New-AzRoleAssignment -ObjectId $ADuser.Id `
        -RoleDefinitionName "GGC Contributor" `
        -ResourceGroupName $groupName
}

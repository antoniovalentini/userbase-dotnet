# userbase-dotnet
A .NET implementation of the userbase client. [More info about the official userbase project here](https://github.com/encrypted-dev/userbase).

This porting is made only for fun and I'm not sure whether there will be future developments or not. I started from the SignIn method, I can produce correct hashes and can properly login, 
but it's not completed yet (i.e. it doesn't store any session).

## Credits
Thanks to [CodesInChaos](https://github.com/CodesInChaos) for the [HKDF C# implementation](https://gist.github.com/CodesInChaos/8710228).
Thanks to James F. Bellinger for the [SCrypt C# implementation](https://www.zer7.com/software/cryptsharp)

## DISCLAIMER
This is a fan-made project only and it's not supported by userbase developers. It's not meant to be used in any production environment and it may be subject to change without notice. 
For all the official userbase releases please refer to the [main website](https://userbase.com/).

## LICENSE
userbase-dotnet is released under the MIT license.

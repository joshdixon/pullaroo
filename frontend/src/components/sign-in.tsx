"use client";

import {Button} from "@/components/ui/button";
import {useRouter} from "next/navigation";

export function SignIn() {
    const router = useRouter();
    
    const handleLoginClick = () => {
        const clientId = "Iv1.5b7afd4137446a44";

        // const codeVerifier = generateCodeVerifier();
        // const codeChallenge = await generateCodeChallenge(codeVerifier);

        const authUrl = `https://github.com/login/oauth/authorize?&client_id=${clientId}`

        router.push(authUrl);
    };
    
    return (
        <Button onClick={handleLoginClick}>Login with GitHub</Button>
    );
};
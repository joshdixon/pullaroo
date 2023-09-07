"use client";

import {useRouter, useSearchParams} from "next/navigation";
import {useEffect} from "react";

export default GitHubCallbackPage() {
    const searchParams = useSearchParams();
    
    const router = useRouter();

    useEffect(() => {
        const code = searchParams.get("code");
    }, []);
    
    return (
        <div>Signing in with GitHub</div>
    )
}

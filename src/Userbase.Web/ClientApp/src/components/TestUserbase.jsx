import React, { Component } from 'react';
import auth from './userbase/auth'

export class TestUserbase extends Component {
    static displayName = TestUserbase.name;

    constructor(props) {
        super(props);
        this.state = { trash: [] };
    }

    componentDidMount() {
        //auth.init({ appId: "YOUR_APP_ID" });
        //this.testSignIn("", "", "");
    }

    render() {
        return (
            <div>
                <h1>Userbase Testing Page</h1>

                <h2>Login</h2>
                <p>username: {this.state.result ? this.state.result.username : ""}</p>
                <p>userId: {this.state.result ? this.state.result.userId : ""}</p>
            </div>
        );
    }

    async testSignIn(username, password, rememberMe) {
        const result = await auth.signIn({
            username,
            password,
            rememberMe
        });
        this.setState({ result });
    }
}

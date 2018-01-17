package com.verbouw.bert.lekkerentjap;

import android.annotation.TargetApi;
import android.graphics.PorterDuff;
import android.graphics.PorterDuffColorFilter;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.Icon;
import android.os.Build;
import android.os.Handler;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.widget.ImageView;
import android.widget.TextView;
import com.loopj.android.http.*;
import pl.pawelkleczkowski.customgauge.CustomGauge;

import java.io.UnsupportedEncodingException;

import cz.msebera.android.httpclient.Header;

public class MainActivity extends AppCompatActivity {

    private int interval = 1500;
    private Handler handler;

    private CustomGauge currenttemp;
    private CustomGauge goaltemp;
    private TextView currenttemptextview;
    private TextView goaltemptextview;
    private TextView pwmon;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        currenttemp = findViewById(R.id.gauge1);
        currenttemptextview = findViewById(R.id.current_temperature_textview);
        goaltemptextview = findViewById(R.id.goal_temperature_textview);
        goaltemp = findViewById(R.id.gauge2);
        pwmon = findViewById(R.id.pwn_textview);

        handler = new Handler();
    }

    @Override
    protected void onResume(){
        super.onResume();
        apiLoop.run();
    }

    @Override
    public void onStop(){
        super.onStop();
        handler.removeCallbacks(apiLoop);
    }

    Runnable apiLoop = new Runnable() {
        @Override
        public void run() {
            try {
                getApiData(); //this function can change value of interval.
            } finally {
                // 100% guarantee that this always happens, even if
                // your update method throws an exception
                handler.postDelayed(apiLoop, interval);
            }
        }
    };


    private void postData(final String[] parts) {
        if(parts.length == 3) {
            currenttemp.post(new Runnable() {
                @Override
                public void run() {
                    currenttemp.setPointSize((Integer.parseInt(parts[0])*270)/100);
                    currenttemp.invalidate();
                }
            });
            currenttemptextview.post(new Runnable() {
                @Override
                public void run() {
                    currenttemptextview.setText(parts[0]+"°");
                }
            });
            goaltemptextview.post(new Runnable() {
                @Override
                public void run() {
                    goaltemptextview.setText("Target: "+parts[1]+"°");
                }
            });
            goaltemp.post(new Runnable() {
                @Override
                public void run() {
                    int startangle = 135+(Integer.parseInt(parts[1])*270)/100;
                    //goaltemp.setStartAngle(startangle);
                    goaltemp.setValue(Integer.parseInt(parts[1]));
                    //goaltemp.invalidate();
                }
            });
            pwmon.post(new Runnable() {
                @Override
                public void run() {
                    if(Boolean.valueOf(parts[2].toLowerCase())){
                        setTextViewDrawableColor(pwmon, R.color.colorAccent);
                        pwmon.setText("PWM On");
                    }else{
                        setTextViewDrawableColor(pwmon, R.color.colorGaugeStroke);
                        pwmon.setText("PWM Off");
                    }
                }
            });
        }
    }

    private void setTextViewDrawableColor(TextView textView, int color) {
        for (Drawable drawable : textView.getCompoundDrawables()) {
            if (drawable != null) {
                drawable.setColorFilter(new PorterDuffColorFilter(getColor(color), PorterDuff.Mode.SRC_IN));
            }
        }
    }

    private void getApiData() {
            ArduinoRestClient.get("arduino/digital/all/", new RequestParams(), new AsyncHttpResponseHandler() {

                @Override
                public void onStart() {
                    // called before request is started
                    Log.println(Log.DEBUG, "response", "Fetching data");
                }

                @Override
                public void onSuccess(int statusCode, Header[] headers, byte[] responseBody) {
                    Log.println(Log.DEBUG, "response", responseBody.toString());
                    try {
                        postData(new String(responseBody, "UTF-8").split(":"));
                    } catch (UnsupportedEncodingException e) {
                        e.printStackTrace();
                    }
                }

                @Override
                public void onFailure(int statusCode, Header[] headers, byte[] errorResponse, Throwable e) {
                    // called when response HTTP status is "4XX" (eg. 401, 403, 404)
                    Log.println(Log.DEBUG, "failed", errorResponse.toString());
                }

                @Override
                public void onRetry(int retryNo) {
                    // called when request is retried
                    Log.println(Log.DEBUG, "retry", "retrying request; count: " + retryNo);
                }
            });

    }
}
